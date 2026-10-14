import argparse
import json
import re
from datetime import datetime, timezone
from pathlib import Path

import requests


HERE = Path(__file__).resolve().parent

FAILURE_ACKNOWLEDGEMENT_PATTERN = re.compile(
    r"\b(could(?:n['’]t| not)|unable|unavailable|failed|failure|"
    r"timed?\s*out|not found|no recent|no telemetry|no data|"
    r"does not exist|doesn['’]t exist)\b",
    re.IGNORECASE,
)
RUNTIME_VALUE_PATTERN = re.compile(
    r"\b\d+(?:\.\d+)?\s*(?:ms|milliseconds?|seconds?|secs?|%|"
    r"percent|rps|requests?\s+per\s+second|connections?)\b",
    re.IGNORECASE,
)


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


def get_required_tools(case):
    if "requiredTools" in case:
        return case["requiredTools"]

    expected_tool = case.get("expectedTool")
    return [expected_tool] if expected_tool else []


def get_allowed_tools(case, required_tools):
    return case.get("allowedTools", required_tools)


def evaluate_expected_failure(
    tool_calls,
    required_tools,
    expected_error_contains,
):
    failures = []

    for tool_name in required_tools:
        candidates = [
            tool_call
            for tool_call in tool_calls
            if tool_call.get("toolName") == tool_name
        ]
        failed_candidates = [
            tool_call
            for tool_call in candidates
            if not tool_call.get("success", False)
        ]

        if not failed_candidates:
            failures.append(
                {
                    "toolName": tool_name,
                    "reason": "The expected tool failure did not occur.",
                }
            )
            continue

        if expected_error_contains and not any(
            expected_error_contains.lower()
            in (candidate.get("error") or "").lower()
            for candidate in failed_candidates
        ):
            failures.append(
                {
                    "toolName": tool_name,
                    "reason": "The tool failed with an unexpected error.",
                    "expectedErrorContains": expected_error_contains,
                    "actualErrors": [
                        candidate.get("error")
                        for candidate in failed_candidates
                    ],
                }
            )

    return not failures, failures


def evaluate_no_data_grounding(answer, must_not_invent_data):
    if not must_not_invent_data:
        return True, []

    failures = []
    if not FAILURE_ACKNOWLEDGEMENT_PATTERN.search(answer):
        failures.append(
            "The answer did not acknowledge that runtime data was unavailable."
        )

    invented_values = RUNTIME_VALUE_PATTERN.findall(answer)
    if invented_values:
        failures.append(
            "The answer contained an unsupported runtime value: "
            + ", ".join(invented_values)
        )

    return not failures, failures


def evaluate_case(case, response):
    tool_calls = response.get("toolCalls", [])
    actual_tools = [tool_call.get("toolName") for tool_call in tool_calls]
    required_tools = get_required_tools(case)
    allowed_tools = get_allowed_tools(case, required_tools)
    expected_arguments = case.get("expectedArguments", {})
    expect_tool_failure = case.get("expectToolFailure", False)
    must_not_invent_data = case.get("mustNotInventData", False)

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
    tool_failure_passed, tool_failure_failures = (
        evaluate_expected_failure(
            tool_calls,
            required_tools,
            case.get("expectedErrorContains"),
        )
        if expect_tool_failure
        else (True, [])
    )
    answer = response.get("answer", "")
    no_data_grounding_passed, grounding_failures = (
        evaluate_no_data_grounding(answer, must_not_invent_data)
    )
    execution_passed = not execution_failures
    execution_outcome_passed = (
        tool_failure_passed
        if expect_tool_failure
        else execution_passed
    )

    result = {
        "id": case["id"],
        "question": case["question"],
        "answer": answer,
        "requiredTools": required_tools,
        "allowedTools": allowed_tools,
        "expectedArguments": expected_arguments,
        "actualTools": actual_tools,
        "toolCalls": tool_calls,
        "missingTools": missing_tools,
        "unexpectedTools": unexpected_tools,
        "argumentFailures": argument_failures,
        "executionFailures": execution_failures,
        "expectToolFailure": expect_tool_failure,
        "toolFailureFailures": tool_failure_failures,
        "mustNotInventData": must_not_invent_data,
        "groundingFailures": grounding_failures,
        "toolFault": case.get("toolFault", "None"),
        "requiredToolsPassed": not missing_tools,
        "noUnnecessaryTools": not unexpected_tools,
        "argumentsPassed": arguments_passed,
        "executionPassed": execution_passed,
        "toolFailurePassed": tool_failure_passed,
        "executionOutcomePassed": execution_outcome_passed,
        "noDataGroundingPassed": no_data_grounding_passed,
    }
    result["passed"] = all(
        [
            result["requiredToolsPassed"],
            result["noUnnecessaryTools"],
            result["argumentsPassed"],
            result["executionOutcomePassed"],
            result["noDataGroundingPassed"],
        ]
    )
    return result


def failed_http_case(case, error):
    required_tools = get_required_tools(case)
    allowed_tools = get_allowed_tools(case, required_tools)

    return {
        "id": case["id"],
        "question": case["question"],
        "answer": "",
        "requiredTools": required_tools,
        "allowedTools": allowed_tools,
        "expectedArguments": case.get("expectedArguments", {}),
        "actualTools": [],
        "toolCalls": [],
        "missingTools": required_tools,
        "unexpectedTools": [],
        "argumentFailures": [],
        "executionFailures": [],
        "requiredToolsPassed": False,
        "noUnnecessaryTools": False,
        "argumentsPassed": False,
        "executionPassed": False,
        "expectToolFailure": case.get("expectToolFailure", False),
        "toolFailurePassed": False,
        "toolFailureFailures": [],
        "executionOutcomePassed": False,
        "mustNotInventData": case.get("mustNotInventData", False),
        "noDataGroundingPassed": False,
        "groundingFailures": [],
        "toolFault": case.get("toolFault", "None"),
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
    successful_execution_cases = [
        result
        for result in results
        if result["requiredTools"] and not result["expectToolFailure"]
    ]
    execution_success_rate = average([
        1 if result["executionPassed"] else 0
        for result in successful_execution_cases
    ])
    expected_failure_cases = [
        result for result in results if result["expectToolFailure"]
    ]
    failure_handling_accuracy = average([
        1 if result["executionOutcomePassed"] else 0
        for result in expected_failure_cases
    ])
    no_data_cases = [
        result for result in results if result["mustNotInventData"]
    ]
    no_data_grounding_accuracy = average([
        1 if result["noDataGroundingPassed"] else 0
        for result in no_data_cases
    ])
    overall_accuracy = average(
        [1 if result["passed"] else 0 for result in results]
    )

    return {
        "caseCount": len(results),
        "requiredToolSelection": tool_selection_accuracy,
        "unnecessaryToolAvoidance": unnecessary_tool_avoidance,
        "argumentAccuracy": argument_accuracy,
        "toolExecutionSuccess": execution_success_rate,
        "expectedFailureHandling": failure_handling_accuracy,
        "noDataGrounding": no_data_grounding_accuracy,
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
        f"Expected failure handling:  "
        f"{summary['expectedFailureHandling']:.1%}"
    )
    print(
        f"No-data grounding:          "
        f"{summary['noDataGrounding']:.1%}"
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

        if failure["toolFailureFailures"]:
            print(
                "Expected failure mismatches:",
                failure["toolFailureFailures"],
            )

        if failure["groundingFailures"]:
            print("Grounding failures:", failure["groundingFailures"])

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
    result_path = args.output_dir / (
        f"{args.dataset.stem}-{timestamp}.json"
    )
    endpoint = f"{args.base_url.rstrip('/')}/api/evals/tool-calling"
    fault_endpoint = (
        f"{args.base_url.rstrip('/')}/api/simulation/tool-fault"
    )
    results = []

    print(f"Running {len(cases)} cases against {endpoint}...")

    with requests.Session() as session:
        for index, case in enumerate(cases, start=1):
            tool_fault = case.get("toolFault", "None")
            try:
                fault_response = session.post(
                    fault_endpoint,
                    json={"fault": tool_fault},
                    timeout=args.timeout,
                )
                fault_response.raise_for_status()

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
            finally:
                if tool_fault != "None":
                    try:
                        reset_response = session.post(
                            fault_endpoint,
                            json={"fault": "None"},
                            timeout=args.timeout,
                        )
                        reset_response.raise_for_status()
                    except requests.RequestException as reset_error:
                        print(
                            "  Warning: could not reset tool fault: "
                            f"{reset_error}"
                        )

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
