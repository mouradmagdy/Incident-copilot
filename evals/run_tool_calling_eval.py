import argparse
import json
from datetime import datetime, timezone
from pathlib import Path

import requests


HERE = Path(__file__).resolve().parent


def parse_args():
    parser = argparse.ArgumentParser(
        description="Evaluate agent tool selection, arguments, and execution."
    )
    parser.add_argument("--base-url", default="http://localhost:5087")
    parser.add_argument(
        "--dataset",
        type=Path,
        default=HERE / "tool-calling.json",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=HERE / "results",
    )
    parser.add_argument("--timeout", type=float, default=300)
    args = parser.parse_args()

    if args.timeout <= 0:
        parser.error("--timeout must be greater than zero")

    return args


def average(values):
    return sum(values) / len(values) if values else 0.0


def is_expected_subset(actual, expected):
    if isinstance(expected, dict):
        return isinstance(actual, dict) and all(
            key in actual and is_expected_subset(actual[key], value)
            for key, value in expected.items()
        )

    if isinstance(expected, list):
        return actual == expected

    return actual == expected


def evaluate_arguments(tool_calls, expected_arguments):
    failures = []

    for tool_name, expected in expected_arguments.items():
        candidates = [
            tool_call
            for tool_call in tool_calls
            if tool_call.get("toolName") == tool_name
        ]
        candidate_arguments = []
        matched = False

        for candidate in candidates:
            arguments_json = candidate.get("argumentsJson", "")
            try:
                arguments = json.loads(arguments_json)
            except (TypeError, json.JSONDecodeError):
                candidate_arguments.append(
                    {
                        "argumentsJson": arguments_json,
                        "parseError": "Invalid JSON",
                    }
                )
                continue

            candidate_arguments.append(arguments)
            if is_expected_subset(arguments, expected):
                matched = True

        if not matched:
            failures.append(
                {
                    "toolName": tool_name,
                    "expected": expected,
                    "actualCandidates": candidate_arguments,
                }
            )

    return not failures, failures


def evaluate_case(case, response):
    tool_calls = response.get("toolCalls", [])
    actual_tools = [tool_call.get("toolName") for tool_call in tool_calls]
    required_tools = case.get("requiredTools", [])
    allowed_tools = case.get("allowedTools", [])
    expected_arguments = case.get("expectedArguments", {})

    missing_tools = sorted(set(required_tools) - set(actual_tools))
    unexpected_tools = [
        tool_name
        for tool_name in actual_tools
        if tool_name not in allowed_tools
    ]
    arguments_passed, argument_failures = evaluate_arguments(
        tool_calls,
        expected_arguments,
    )
    execution_failures = [
        {
            "toolName": tool_call.get("toolName"),
            "error": tool_call.get("error"),
        }
        for tool_call in tool_calls
        if not tool_call.get("success", False)
    ]

    result = {
        "id": case["id"],
        "question": case["question"],
        "answer": response.get("answer", ""),
        "requiredTools": required_tools,
        "allowedTools": allowed_tools,
        "expectedArguments": expected_arguments,
        "actualTools": actual_tools,
        "toolCalls": tool_calls,
        "missingTools": missing_tools,
        "unexpectedTools": unexpected_tools,
        "argumentFailures": argument_failures,
        "executionFailures": execution_failures,
        "requiredToolsPassed": not missing_tools,
        "noUnnecessaryTools": not unexpected_tools,
        "argumentsPassed": arguments_passed,
        "executionPassed": not execution_failures,
    }
    result["passed"] = all(
        [
            result["requiredToolsPassed"],
            result["noUnnecessaryTools"],
            result["argumentsPassed"],
            result["executionPassed"],
        ]
    )
    return result


def failed_http_case(case, error):
    return {
        "id": case["id"],
        "question": case["question"],
        "answer": "",
        "requiredTools": case.get("requiredTools", []),
        "allowedTools": case.get("allowedTools", []),
        "expectedArguments": case.get("expectedArguments", {}),
        "actualTools": [],
        "toolCalls": [],
        "missingTools": case.get("requiredTools", []),
        "unexpectedTools": [],
        "argumentFailures": [],
        "executionFailures": [],
        "requiredToolsPassed": False,
        "noUnnecessaryTools": False,
        "argumentsPassed": False,
        "executionPassed": False,
        "passed": False,
        "endpointError": str(error),
    }


def calculate_summary(results):
    tool_selection_accuracy = average(
        [1 if result["requiredToolsPassed"] else 0 for result in results]
    )
    unnecessary_tool_avoidance = average(
        [1 if result["noUnnecessaryTools"] else 0 for result in results]
    )
    argument_accuracy = average(
        [1 if result["argumentsPassed"] else 0 for result in results]
    )
    execution_success_rate = average(
        [1 if result["executionPassed"] else 0 for result in results]
    )
    overall_accuracy = average(
        [1 if result["passed"] else 0 for result in results]
    )

    return {
        "caseCount": len(results),
        "requiredToolSelection": tool_selection_accuracy,
        "unnecessaryToolAvoidance": unnecessary_tool_avoidance,
        "argumentAccuracy": argument_accuracy,
        "toolExecutionSuccess": execution_success_rate,
        "overallCaseAccuracy": overall_accuracy,
    }


def create_run(args, started_at, results, completed_at=None):
    run = {
        "startedAtUtc": started_at,
        "completedAtUtc": completed_at,
        "baseUrl": args.base_url,
        "dataset": str(args.dataset.resolve()),
        "summary": calculate_summary(results),
        "results": results,
    }
    return run


def save_run(path, run):
    path.write_text(
        json.dumps(run, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )


def print_summary(summary):
    print()
    print("=" * 60)
    print("TOOL CALLING EVALUATION")
    print("=" * 60)
    print(
        f"Required tool selection:     "
        f"{summary['requiredToolSelection']:.1%}"
    )
    print(
        f"Unnecessary tool avoidance: "
        f"{summary['unnecessaryToolAvoidance']:.1%}"
    )
    print(
        f"Argument accuracy:          "
        f"{summary['argumentAccuracy']:.1%}"
    )
    print(
        f"Tool execution success:     "
        f"{summary['toolExecutionSuccess']:.1%}"
    )
    print(
        f"Overall case accuracy:      "
        f"{summary['overallCaseAccuracy']:.1%}"
    )


def print_failures(results):
    failures = [result for result in results if not result["passed"]]

    print()
    print("=" * 60)
    print(f"FAILURES: {len(failures)}")
    print("=" * 60)

    for failure in failures:
        print()
        print(f"ID:       {failure['id']}")
        print(f"Question: {failure['question']}")
        print("Expected:", failure["requiredTools"])
        print("Actual:  ", failure["actualTools"])

        if failure["missingTools"]:
            print("Missing: ", failure["missingTools"])

        if failure["unexpectedTools"]:
            print("Unexpected:", failure["unexpectedTools"])

        if failure["argumentFailures"]:
            print(
                "Argument failures:",
                json.dumps(
                    failure["argumentFailures"],
                    indent=2,
                    ensure_ascii=False,
                ),
            )

        if failure["executionFailures"]:
            print("Execution failures:", failure["executionFailures"])

        if failure.get("endpointError"):
            print("Endpoint error:", failure["endpointError"])


def main():
    args = parse_args()
    with args.dataset.open("r", encoding="utf-8") as file:
        cases = json.load(file)

    args.output_dir.mkdir(parents=True, exist_ok=True)
    started = datetime.now(timezone.utc)
    started_at = started.isoformat()
    timestamp = started.strftime("%Y%m%dT%H%M%S%fZ")
    result_path = args.output_dir / f"tool-calling-{timestamp}.json"
    endpoint = f"{args.base_url.rstrip('/')}/api/evals/tool-calling"
    results = []

    print(f"Running {len(cases)} cases against {endpoint}...")

    with requests.Session() as session:
        for index, case in enumerate(cases, start=1):
            try:
                response = session.post(
                    endpoint,
                    json={"message": case["question"]},
                    timeout=args.timeout,
                )
                response.raise_for_status()
                result = evaluate_case(case, response.json())
            except requests.HTTPError as error:
                response_body = (
                    error.response.text
                    if error.response is not None
                    else ""
                )
                detail = str(error)
                if response_body:
                    detail = f"{detail}; response: {response_body}"
                result = failed_http_case(case, detail)
            except (requests.RequestException, ValueError) as error:
                result = failed_http_case(case, error)

            results.append(result)
            save_run(
                result_path,
                create_run(args, started_at, results),
            )
            status = "PASS" if result["passed"] else "FAIL"
            print(f"  [{index:02}/{len(cases)}] {case['id']}: {status}")

    completed_at = datetime.now(timezone.utc).isoformat()
    run = create_run(args, started_at, results, completed_at)
    save_run(result_path, run)

    print_summary(run["summary"])
    print_failures(results)
    print()
    print(f"Saved complete results: {result_path}")


if __name__ == "__main__":
    main()
