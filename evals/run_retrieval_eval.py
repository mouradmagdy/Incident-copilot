import json
from pathlib import Path

import requests


# -----------------------------
# Configuration
# -----------------------------

BASE_URL = "http://localhost:5087"
SEARCH_URL = f"{BASE_URL}/api/knowledge/search"

TOP_K = 5

# Initial experimental threshold.
# Do not treat this as final; use the printed distance distributions
# to decide whether it should change.
MAX_RELEVANT_DISTANCE = 0.75

HERE = Path(__file__).resolve().parent
DATASET_PATH = HERE / "retrieval.json"
RESULTS_PATH = HERE / "retrieval-results.json"


# -----------------------------
# Helpers
# -----------------------------

def hit_at_k(retrieved_documents, expected_document, k):
    """True when the expected document appears in the first k returned chunks."""
    return expected_document in retrieved_documents[:k]


def first_rank(retrieved_documents, expected_document):
    """
    Return the 1-based rank of the first chunk from expected_document.
    Return None when it was not retrieved.
    """
    try:
        return retrieved_documents.index(expected_document) + 1
    except ValueError:
        return None


def search(question, top_k):
    response = requests.get(
        SEARCH_URL,
        params={
            "query": question,
            "topK": top_k,
        },
        timeout=30,
    )
    response.raise_for_status()
    return response.json()


# -----------------------------
# Load eval cases
# -----------------------------

with DATASET_PATH.open("r", encoding="utf-8") as f:
    cases = json.load(f)


results = []

print(f"Running {len(cases)} retrieval eval cases...")
print(f"Endpoint: {SEARCH_URL}")
print(f"Top K: {TOP_K}")
print()


# -----------------------------
# Run retrieval
# -----------------------------

for index, case in enumerate(cases, start=1):
    chunks = search(case["question"], TOP_K)

    retrieved_documents = [
        chunk["fileName"]
        for chunk in chunks
    ]

    distances = [
        chunk["distance"]
        for chunk in chunks
    ]

    best_distance = min(distances) if distances else None

    expected_document = case["expectedDocument"]

    result = {
        "id": case["id"],
        "question": case["question"],
        "expectedDocument": expected_document,
        "retrievedDocuments": retrieved_documents,
        "distances": distances,
        "bestDistance": best_distance,
    }

    if expected_document is not None:
        result["rank"] = first_rank(
            retrieved_documents,
            expected_document,
        )

        result["hitAt1"] = hit_at_k(
            retrieved_documents,
            expected_document,
            1,
        )

        result["hitAt3"] = hit_at_k(
            retrieved_documents,
            expected_document,
            3,
        )

        result["hitAt5"] = hit_at_k(
            retrieved_documents,
            expected_document,
            5,
        )
    else:
        # For an irrelevant query, we ideally want no result to pass
        # our relevance threshold.
        result["correctlyRejected"] = (
            best_distance is None
            or best_distance > MAX_RELEVANT_DISTANCE
        )

    results.append(result)

    print(f"[{index:02}/{len(cases)}] {case['id']}")


# -----------------------------
# Relevant-query metrics
# -----------------------------

relevant_results = [
    r for r in results
    if r["expectedDocument"] is not None
]

irrelevant_results = [
    r for r in results
    if r["expectedDocument"] is None
]


def average(values):
    return sum(values) / len(values) if values else 0.0


recall_at_1 = average([
    1 if r["hitAt1"] else 0
    for r in relevant_results
])

recall_at_3 = average([
    1 if r["hitAt3"] else 0
    for r in relevant_results
])

recall_at_5 = average([
    1 if r["hitAt5"] else 0
    for r in relevant_results
])


# Mean Reciprocal Rank (optional but useful).
reciprocal_ranks = []

for result in relevant_results:
    rank = result["rank"]

    reciprocal_ranks.append(
        0.0 if rank is None else 1.0 / rank
    )

mrr = average(reciprocal_ranks)


# -----------------------------
# Irrelevant-query metrics
# -----------------------------

rejection_accuracy = average([
    1 if r["correctlyRejected"] else 0
    for r in irrelevant_results
])


# -----------------------------
# Distance summaries
# -----------------------------

relevant_best_distances = [
    r["bestDistance"]
    for r in relevant_results
    if r["bestDistance"] is not None
]

irrelevant_best_distances = [
    r["bestDistance"]
    for r in irrelevant_results
    if r["bestDistance"] is not None
]


print()
print("=" * 60)
print("RETRIEVAL EVALUATION")
print("=" * 60)

print(f"Relevant cases:   {len(relevant_results)}")
print(f"Irrelevant cases: {len(irrelevant_results)}")
print()

print(f"Recall@1: {recall_at_1:.1%}")
print(f"Recall@3: {recall_at_3:.1%}")
print(f"Recall@5: {recall_at_5:.1%}")
print(f"MRR:      {mrr:.3f}")

print()
print(
    f"Irrelevant-query rejection @ distance > "
    f"{MAX_RELEVANT_DISTANCE:.2f}: "
    f"{rejection_accuracy:.1%}"
)


if relevant_best_distances:
    print()
    print("Relevant-query nearest distances:")
    print(f"  min: {min(relevant_best_distances):.4f}")
    print(f"  max: {max(relevant_best_distances):.4f}")
    print(
        f"  avg: "
        f"{average(relevant_best_distances):.4f}"
    )


if irrelevant_best_distances:
    print()
    print("Irrelevant-query nearest distances:")
    print(f"  min: {min(irrelevant_best_distances):.4f}")
    print(f"  max: {max(irrelevant_best_distances):.4f}")
    print(
        f"  avg: "
        f"{average(irrelevant_best_distances):.4f}"
    )


# -----------------------------
# Failure analysis
# -----------------------------

failures_at_3 = [
    r for r in relevant_results
    if not r["hitAt3"]
]

print()
print("=" * 60)
print(f"FAILURES AT TOP 3: {len(failures_at_3)}")
print("=" * 60)

if not failures_at_3:
    print("None.")
else:
    for result in failures_at_3:
        print()
        print(f"ID:       {result['id']}")
        print(f"Question: {result['question']}")
        print(f"Expected: {result['expectedDocument']}")

        print("Retrieved:")

        for rank, (document, distance) in enumerate(
            zip(
                result["retrievedDocuments"],
                result["distances"],
            ),
            start=1,
        ):
            print(
                f"  {rank}. {document} "
                f"(distance={distance:.4f})"
            )


false_accepts = [
    r for r in irrelevant_results
    if not r["correctlyRejected"]
]

print()
print("=" * 60)
print(f"IRRELEVANT QUERIES NOT REJECTED: {len(false_accepts)}")
print("=" * 60)

if not false_accepts:
    print("None.")
else:
    for result in false_accepts:
        print()
        print(f"ID:           {result['id']}")
        print(f"Question:     {result['question']}")
        print(
            f"Best distance: "
            f"{result['bestDistance']:.4f}"
        )

        if result["retrievedDocuments"]:
            print(
                f"Nearest doc:   "
                f"{result['retrievedDocuments'][0]}"
            )


# -----------------------------
# Persist raw results
# -----------------------------

summary = {
    "topK": TOP_K,
    "maxRelevantDistance": MAX_RELEVANT_DISTANCE,
    "relevantCases": len(relevant_results),
    "irrelevantCases": len(irrelevant_results),
    "recallAt1": recall_at_1,
    "recallAt3": recall_at_3,
    "recallAt5": recall_at_5,
    "mrr": mrr,
    "irrelevantRejectionAccuracy": rejection_accuracy,
    "relevantNearestDistance": {
        "min": (
            min(relevant_best_distances)
            if relevant_best_distances
            else None
        ),
        "max": (
            max(relevant_best_distances)
            if relevant_best_distances
            else None
        ),
        "avg": (
            average(relevant_best_distances)
            if relevant_best_distances
            else None
        ),
    },
    "irrelevantNearestDistance": {
        "min": (
            min(irrelevant_best_distances)
            if irrelevant_best_distances
            else None
        ),
        "max": (
            max(irrelevant_best_distances)
            if irrelevant_best_distances
            else None
        ),
        "avg": (
            average(irrelevant_best_distances)
            if irrelevant_best_distances
            else None
        ),
    },
}

output = {
    "summary": summary,
    "results": results,
}

with RESULTS_PATH.open("w", encoding="utf-8") as f:
    json.dump(
        output,
        f,
        indent=2,
        ensure_ascii=False,
    )

print()
print(f"Raw results saved to: {RESULTS_PATH}")
