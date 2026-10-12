import argparse
import json
from pathlib import Path

import requests


HERE = Path(__file__).resolve().parent


def parse_chunking(value):
    try:
        target_size, overlap = (int(part) for part in value.split(":", 1))
    except (TypeError, ValueError) as error:
        raise argparse.ArgumentTypeError(
            "chunking must use TARGET:OVERLAP, for example 400:50"
        ) from error

    if target_size <= 0 or overlap < 0 or overlap >= target_size:
        raise argparse.ArgumentTypeError(
            "target must be positive and overlap must be between 0 and target - 1"
        )

    return target_size, overlap


def parse_args():
    parser = argparse.ArgumentParser(
        description="Evaluate retrieval, optionally reindexing multiple chunk configurations."
    )
    parser.add_argument("--base-url", default="http://localhost:5087")
    parser.add_argument("--dataset", type=Path, default=HERE / "retrieval.json")
    parser.add_argument("--output-dir", type=Path, default=HERE)
    parser.add_argument("--top-k", type=int, default=5)
    parser.add_argument("--max-distance", type=float, default=0.75)
    parser.add_argument(
        "--chunking",
        action="append",
        type=parse_chunking,
        metavar="TARGET:OVERLAP",
        help=(
            "Reindex and evaluate this configuration. Repeat to compare several "
            "configurations. With no value, evaluate the existing index."
        ),
    )
    args = parser.parse_args()

    if args.top_k <= 0:
        parser.error("--top-k must be greater than zero")

    return args


def average(values):
    return sum(values) / len(values) if values else 0.0


def distance_summary(values):
    return {
        "min": min(values) if values else None,
        "max": max(values) if values else None,
        "avg": average(values) if values else None,
    }


def reindex(session, base_url, chunking):
    target_size, overlap = chunking
    response = session.post(
        f"{base_url.rstrip('/')}/api/knowledge/reindex",
        params={"targetChunkSize": target_size, "chunkOverlap": overlap},
        timeout=300,
    )
    response.raise_for_status()


def search(session, base_url, question, top_k):
    response = session.get(
        f"{base_url.rstrip('/')}/api/knowledge/search",
        params={"query": question, "topK": top_k},
        timeout=30,
    )
    response.raise_for_status()
    return response.json()


def evaluate(session, args, cases, chunking=None):
    if chunking:
        reindex(session, args.base_url, chunking)
    results = []

    for index, case in enumerate(cases, start=1):
        chunks = search(session, args.base_url, case["question"], args.top_k)
        documents = [chunk["fileName"] for chunk in chunks]
        distances = [chunk["distance"] for chunk in chunks]
        best_distance = min(distances) if distances else None
        expected = case["expectedDocument"]

        result = {
            "id": case["id"],
            "question": case["question"],
            "expectedDocument": expected,
            "retrievedDocuments": documents,
            "distances": distances,
            "bestDistance": best_distance,
        }

        if expected is not None:
            rank = documents.index(expected) + 1 if expected in documents else None
            result.update(
                {
                    "rank": rank,
                    "hitAt1": expected in documents[:1],
                    "hitAt3": expected in documents[:3],
                    "hitAt5": expected in documents[:5],
                }
            )
        else:
            result["correctlyRejected"] = (
                best_distance is None or best_distance > args.max_distance
            )

        results.append(result)
        print(f"  [{index:02}/{len(cases)}] {case['id']}")

    relevant = [item for item in results if item["expectedDocument"] is not None]
    irrelevant = [item for item in results if item["expectedDocument"] is None]
    relevant_distances = [
        item["bestDistance"] for item in relevant if item["bestDistance"] is not None
    ]
    irrelevant_distances = [
        item["bestDistance"] for item in irrelevant if item["bestDistance"] is not None
    ]

    summary = {
        "topK": args.top_k,
        "maxRelevantDistance": args.max_distance,
        "relevantCases": len(relevant),
        "irrelevantCases": len(irrelevant),
        "recallAt1": average([int(item["hitAt1"]) for item in relevant]),
        "recallAt3": average([int(item["hitAt3"]) for item in relevant]),
        "recallAt5": average([int(item["hitAt5"]) for item in relevant]),
        "mrr": average(
            [0.0 if item["rank"] is None else 1.0 / item["rank"] for item in relevant]
        ),
        "irrelevantRejectionAccuracy": average(
            [int(item["correctlyRejected"]) for item in irrelevant]
        ),
        "relevantNearestDistance": distance_summary(relevant_distances),
        "irrelevantNearestDistance": distance_summary(irrelevant_distances),
    }

    return {
        "chunking": (
            {"targetChunkSize": chunking[0], "chunkOverlap": chunking[1]}
            if chunking
            else None
        ),
        "summary": summary,
        "results": results,
    }


def print_summary(run):
    summary = run["summary"]
    chunking = run["chunking"]
    label = (
        f"{chunking['targetChunkSize']}/{chunking['chunkOverlap']}"
        if chunking
        else "existing index"
    )
    print(f"\nResults for {label}")
    print(f"  Recall@1: {summary['recallAt1']:.1%}")
    print(f"  Recall@3: {summary['recallAt3']:.1%}")
    print(f"  Recall@5: {summary['recallAt5']:.1%}")
    print(f"  MRR:      {summary['mrr']:.3f}")
    print(f"  Irrelevant rejection: {summary['irrelevantRejectionAccuracy']:.1%}")


def run_score(run):
    summary = run["summary"]
    return (
        summary["recallAt3"],
        summary["mrr"],
        summary["irrelevantRejectionAccuracy"],
        summary["recallAt1"],
    )


def main():
    args = parse_args()
    with args.dataset.open("r", encoding="utf-8") as file:
        cases = json.load(file)

    args.output_dir.mkdir(parents=True, exist_ok=True)
    configurations = args.chunking or [None]
    runs = []

    print(f"Running {len(cases)} cases against {args.base_url}...")
    with requests.Session() as session:
        for chunking in configurations:
            if chunking:
                print(f"\nReindexing with target/overlap {chunking[0]}/{chunking[1]}...")
            run = evaluate(session, args, cases, chunking)
            runs.append(run)
            print_summary(run)

            suffix = f"-{chunking[0]}-{chunking[1]}" if chunking else ""
            result_path = args.output_dir / f"retrieval-results{suffix}.json"
            result_path.write_text(
                json.dumps(run, indent=2, ensure_ascii=False), encoding="utf-8"
            )
            print(f"  Saved: {result_path}")

    if len(runs) > 1:
        best = max(runs, key=run_score)
        comparison = {
            "selectionOrder": [
                "recallAt3",
                "mrr",
                "irrelevantRejectionAccuracy",
                "recallAt1",
            ],
            "recommended": best["chunking"],
            "runs": [
                {
                    "chunking": run["chunking"],
                    "summary": run["summary"],
                }
                for run in runs
            ],
        }
        comparison_path = args.output_dir / "retrieval-comparison.json"
        comparison_path.write_text(
            json.dumps(comparison, indent=2, ensure_ascii=False), encoding="utf-8"
        )
        selected = best["chunking"]
        print(
            f"\nRecommended by retrieval score: "
            f"{selected['targetChunkSize']}/{selected['chunkOverlap']}"
        )
        print(f"Comparison saved: {comparison_path}")


if __name__ == "__main__":
    try:
        main()
    except requests.RequestException as error:
        raise SystemExit(f"HTTP request failed: {error}") from error
