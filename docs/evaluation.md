# Evaluation summary

This page summarizes the committed evaluation artifacts. It reports observed results for small, curated datasets; it does not claim statistical significance or production safety.

## Results

| Area | Dataset / runs | Committed result | What it supports |
| --- | ---: | --- | --- |
| Retrieval | 30 cases: 25 relevant, 5 irrelevant | Recall@1 88%, recall@3 96%, recall@5 100%, MRR 0.928; irrelevant rejection 100% at distance 0.75 | The five included runbooks are usually retrieved near the top for the authored questions. |
| Tool calling | 24 cases | 100% required-tool selection, unnecessary-tool avoidance, argument accuracy, execution success, and overall case accuracy | The evaluated model/configuration followed the expected read/write tool routes on this dataset. |
| Prompt injection / security | 10 cases | 60% overall pass rate; 0 unauthorized direct writes, 0 approval bypasses, 3 false action-completion claims | The approval boundary held in this run, but response-grounding failures remain and the suite does not establish security. |
| Injected runtime failures | 3 cases | 66.7% overall accuracy; expected-failure handling 100%; no-data grounding 66.7%; tool execution success 0% because every case injects a failure | Errors were surfaced in all three cases, but one response was not adequately grounded in the absence of data. |
| Latency | 30 successful runs: 10 per scenario | See scenario table below | A local baseline for one API/model/network configuration, not an SLO. |

The retrieval run used top-K 5 for measurement, while the live chat path currently requests top-K 4; both use a maximum relevant distance of 0.75. The difference should be kept in mind when comparing offline recall with UI behavior.

### Latency baseline

| Scenario | Successes | Mean | p50 | p95 | Mean time to first event |
| --- | ---: | ---: | ---: | ---: | ---: |
| RAG only | 10/10 | 1,931 ms | 1,804 ms | 3,190 ms | 1,452 ms |
| One tool | 10/10 | 1,589 ms | 1,526 ms | 2,362 ms | 1,526 ms |
| Multi-step investigation | 10/10 | 6,223 ms | 5,262 ms | 11,576 ms | 4,087 ms |

The baseline was recorded on 2026-10-15 against `http://localhost:5087`, with 20 seconds between runs plus up to 2 seconds of jitter and 60 seconds between scenarios. Provider latency, model behavior, local hardware, dataset state, and network conditions can all materially change these numbers.

## Artifacts

- Retrieval: `evals/retrieval.json` and `evals/retrieval-results.json`
- Tool calling: `evals/tool-calling.json` and `evals/results/tool-calling-20261014T131429842470Z.json`
- Prompt injection: `evals/prompt-injection.json` and `evals/results/prompt-injection-20261016T143259783711Z.json`
- Runtime failures: `evals/runtime-failures.json` and `evals/results/runtime-failures-20261014T133816495474Z.json`
- Latency: `evals/results/latency-baseline-20261015T150453695833Z.json`
- Manual security notes: `docs/security-tests/prompt-injection.md`

## Reproduction

Start the API, database, and embedding service; index the runbooks; then inspect each script's options:

```powershell
python evals/run_retrieval_eval.py --help
python evals/run_tool_calling_eval.py --help
python evals/run_prompt_injection_eval.py --help
python evals/run_latency_eval.py --help
```

The tool, prompt-injection, runtime-failure, and latency results depend on a live LLM and may vary across runs. Preserve raw output artifacts when comparing changes, and do not replace dataset sizes with percentages alone.

## Gaps

The current suite does not measure multi-turn conversational quality, authorization/tenant boundaries, load or concurrency, recovery after process failure, long-context behavior, cost, or adversarial coverage beyond the ten authored security cases. Known false-completion claims and the runtime no-data grounding miss should be treated as open quality risks.
