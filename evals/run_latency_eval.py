import argparse
import json
import math
import random
import statistics
import time
from datetime import datetime, timezone
from email.utils import parsedate_to_datetime
from pathlib import Path

import requests


HERE = Path(__file__).resolve().parent

QUESTIONS = {
    "rag_only": "What symptoms indicate database connection exhaustion?",
    "one_tool": "What is OrderService's current p95 latency?",
    "investigation": (
        "Check OrderService's current metrics and recent logs and tell me "
        "why it is unhealthy."
    ),
}


def parse_args():
    parser = argparse.ArgumentParser(
        description=(
            "Benchmark complete streamed chat latency for RAG and tool workloads."
        )
    )
    parser.add_argument("--base-url", default="http://localhost:5087")
    parser.add_argument("--runs", type=int, default=10)
    parser.add_argument(
        "--pause",
        type=float,
        default=20.0,
        help="Seconds between successful requests (default: 20).",
    )
    parser.add_argument(
        "--jitter",
        type=float,
        default=2.0,
        help="Random seconds added to each pause (default: up to 2).",
    )
    parser.add_argument(
        "--scenario-pause",
        type=float,
        default=60.0,
        help="Additional cooldown between workloads (default: 60).",
    )
    parser.add_argument("--timeout", type=float, default=180.0)
    parser.add_argument(
        "--max-attempts",
        type=int,
        default=3,
        help="Maximum attempts for a failed benchmark run (default: 3).",
    )
    parser.add_argument(
        "--failure-backoff",
        type=float,
        default=60.0,
        help="Initial backoff for retryable failures without Retry-After.",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=HERE / "results",
    )
    parser.add_argument(
        "--scenario",
        action="append",
        choices=QUESTIONS,
        help="Run only this scenario; repeat to select multiple scenarios.",
    )
    args = parser.parse_args()

    if args.runs <= 0:
        parser.error("--runs must be greater than zero")
    if args.pause < 0 or args.jitter < 0 or args.scenario_pause < 0:
        parser.error("pause values cannot be negative")
    if args.timeout <= 0:
        parser.error("--timeout must be greater than zero")
    if args.max_attempts <= 0:
        parser.error("--max-attempts must be greater than zero")
    if args.failure_backoff < 0:
        parser.error("--failure-backoff cannot be negative")

    return args


def percentile(values, quantile):
    """Return a linearly interpolated percentile from successful durations."""
    if not values:
        return None

    ordered = sorted(values)
    position = (len(ordered) - 1) * quantile
    lower_index = math.floor(position)
    upper_index = math.ceil(position)

    if lower_index == upper_index:
        return ordered[lower_index]

    weight = position - lower_index
    return (
        ordered[lower_index] * (1.0 - weight)
        + ordered[upper_index] * weight
    )


def retry_after_seconds(response):
    value = response.headers.get("Retry-After")
    if not value:
        return None

    try:
        return max(0.0, float(value))
    except ValueError:
        try:
            retry_at = parsedate_to_datetime(value)
            if retry_at.tzinfo is None:
                retry_at = retry_at.replace(tzinfo=timezone.utc)
            return max(
                0.0,
                (retry_at - datetime.now(timezone.utc)).total_seconds(),
            )
        except (TypeError, ValueError, OverflowError):
            return None


def consume_chat_stream(response, request_started):
    first_event_ms = None
    event_count = 0
    saw_sources = False

    for line in response.iter_lines(decode_unicode=True):
        if not line:
            continue

        if first_event_ms is None:
            first_event_ms = (
                time.perf_counter() - request_started
            ) * 1000

        event = json.loads(line)
        event_count += 1
        if event.get("type") == "sources":
            saw_sources = True

    if not saw_sources:
        raise RuntimeError(
            "The chat stream ended before the final sources event."
        )

    return first_event_ms, event_count


def run_once(session, url, question, timeout):
    started = time.perf_counter()

    with session.post(
        url,
        json={"message": question},
        headers={"Accept": "application/x-ndjson"},
        stream=True,
        timeout=timeout,
    ) as response:
        if response.status_code == 429:
            return {
                "success": False,
                "statusCode": response.status_code,
                "retryAfterSeconds": retry_after_seconds(response),
                "error": "The Incident Copilot API returned HTTP 429.",
            }

        response.raise_for_status()
        first_event_ms, event_count = consume_chat_stream(
            response,
            started,
        )

    elapsed_ms = (time.perf_counter() - started) * 1000
    return {
        "success": True,
        "statusCode": response.status_code,
        "durationMs": elapsed_ms,
        "timeToFirstEventMs": first_event_ms,
        "eventCount": event_count,
    }


def save_results(path, started_at, args, scenario_results):
    payload = {
        "startedAtUtc": started_at,
        "updatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "baseUrl": args.base_url,
        "configuredRunsPerScenario": args.runs,
        "pauseSeconds": args.pause,
        "jitterSeconds": args.jitter,
        "scenarioPauseSeconds": args.scenario_pause,
        "results": scenario_results,
    }
    path.write_text(
        json.dumps(payload, indent=2),
        encoding="utf-8",
    )


def summarize(name, question, runs):
    successful = [run for run in runs if run["success"]]
    durations = [run["durationMs"] for run in successful]
    first_events = [
        run["timeToFirstEventMs"]
        for run in successful
        if run["timeToFirstEventMs"] is not None
    ]

    return {
        "scenario": name,
        "question": question,
        "requestedRuns": len(runs),
        "successfulRuns": len(successful),
        "failedRuns": len(runs) - len(successful),
        "meanMs": statistics.mean(durations) if durations else None,
        "minMs": min(durations) if durations else None,
        "maxMs": max(durations) if durations else None,
        "p50Ms": percentile(durations, 0.50),
        "p95Ms": percentile(durations, 0.95),
        "p99Ms": percentile(durations, 0.99),
        "meanTimeToFirstEventMs": (
            statistics.mean(first_events) if first_events else None
        ),
        "durationsMs": durations,
        "runs": runs,
    }


def print_duration(label, value):
    rendered = "n/a" if value is None else f"{value:.0f} ms"
    print(f"{label:<6}{rendered}")


def benchmark_scenario(
    session,
    args,
    name,
    question,
    checkpoint=None,
):
    print()
    print("=" * 60)
    print(name)
    print("=" * 60)

    url = f"{args.base_url.rstrip('/')}/api/chat"
    runs = []

    for run_number in range(1, args.runs + 1):
        attempts = []
        final_result = None

        for attempt_number in range(1, args.max_attempts + 1):
            try:
                result = run_once(
                    session,
                    url,
                    question,
                    args.timeout,
                )
            except (requests.RequestException, RuntimeError, json.JSONDecodeError) as error:
                result = {
                    "success": False,
                    "statusCode": None,
                    "retryAfterSeconds": None,
                    "error": str(error),
                }

            result["attempt"] = attempt_number
            attempts.append(result)

            if result["success"]:
                final_result = result
                break

            if attempt_number == args.max_attempts:
                break

            delay = result.get("retryAfterSeconds")
            if delay is None:
                delay = args.failure_backoff * (2 ** (attempt_number - 1))

            print(
                f"  [{run_number:02}/{args.runs}] attempt "
                f"{attempt_number} failed: {result['error']}"
            )
            print(f"       retrying after {delay:.1f} seconds")
            time.sleep(delay)

        if final_result is None:
            run_result = {
                "run": run_number,
                "success": False,
                "error": attempts[-1]["error"],
                "attempts": attempts,
            }
            print(f"  [{run_number:02}/{args.runs}] FAILED")
        else:
            run_result = {
                "run": run_number,
                "success": True,
                "durationMs": final_result["durationMs"],
                "timeToFirstEventMs": final_result["timeToFirstEventMs"],
                "eventCount": final_result["eventCount"],
                "attempts": attempts,
            }
            print(
                f"  [{run_number:02}/{args.runs}] "
                f"{final_result['durationMs']:.0f} ms"
            )

        runs.append(run_result)

        if checkpoint is not None:
            checkpoint(summarize(name, question, runs))

        if run_number < args.runs:
            time.sleep(args.pause + random.uniform(0, args.jitter))

    result = summarize(name, question, runs)
    print()
    print_duration("mean:", result["meanMs"])
    print_duration("p50:", result["p50Ms"])
    print_duration("p95:", result["p95Ms"])
    print_duration("p99:", result["p99Ms"])
    print_duration("min:", result["minMs"])
    print_duration("max:", result["maxMs"])
    return result


def main():
    args = parse_args()
    args.output_dir.mkdir(parents=True, exist_ok=True)

    started = datetime.now(timezone.utc)
    timestamp = started.strftime("%Y%m%dT%H%M%S%fZ")
    output_path = args.output_dir / f"latency-baseline-{timestamp}.json"
    selected = args.scenario or list(QUESTIONS)
    scenario_results = []

    print(
        f"Running {args.runs} request(s) for {len(selected)} scenario(s) "
        f"against {args.base_url.rstrip('/')}..."
    )
    print(
        "Requests run sequentially with a "
        f"{args.pause:.1f}s + up to {args.jitter:.1f}s cooldown."
    )
    print(f"Partial results will be saved to {output_path}")

    with requests.Session() as session:
        for index, name in enumerate(selected):
            def checkpoint(partial_result):
                save_results(
                    output_path,
                    started.isoformat(),
                    args,
                    scenario_results + [partial_result],
                )

            result = benchmark_scenario(
                session,
                args,
                name,
                QUESTIONS[name],
                checkpoint,
            )
            scenario_results.append(result)
            save_results(
                output_path,
                started.isoformat(),
                args,
                scenario_results,
            )

            if index < len(selected) - 1 and args.scenario_pause > 0:
                print(
                    f"Cooling down for {args.scenario_pause:.1f} seconds "
                    "before the next workload..."
                )
                time.sleep(args.scenario_pause)

    print()
    print(f"Saved complete results: {output_path.resolve()}")


if __name__ == "__main__":
    main()
