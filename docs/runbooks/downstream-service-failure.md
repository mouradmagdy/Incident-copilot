# Downstream Service Failure

A downstream service failure occurs when a dependency called by one or more services becomes unavailable, slow, or returns invalid responses. The caller may be healthy but still show increased latency and errors. Failures can cascade when callers retry aggressively or hold resources while waiting.

## Common symptoms

- Dependency requests time out, fail to connect, or return many HTTP 5xx responses.
- Caller p95 latency and error rate rise while its CPU remains normal.
- Circuit breakers open and retry counts increase.
- Queues or thread pools grow because work is waiting on the dependency.
- Multiple callers fail at approximately the same time.

## Investigation

1. Identify the failing dependency, affected operations, callers, regions, and start time.
2. Compare direct dependency health with caller-side metrics and distributed traces.
3. Check DNS, certificates, network routes, authentication, quotas, and rate limits.
4. Review the dependency's latest deployments, configuration changes, and capacity signals.
5. Inspect timeout and retry settings. Verify that retry storms are not amplifying the outage.
6. Determine whether responses are completely unavailable, intermittently failing, slow, or malformed.
7. Contact the dependency owner and establish a shared incident channel when ownership differs.

## Mitigation

- Enable a documented fallback, cached response, or degraded mode where correctness permits.
- Shed or rate-limit nonessential requests and use circuit breakers to fail quickly.
- Reduce retries or add exponential backoff with jitter. Never retry non-idempotent operations unless they have an idempotency guarantee.
- Shift traffic to a healthy region or provider when a tested failover path exists.
- Roll back a caller change if it caused incompatible requests or excessive load.
- Preserve failed work for safe replay rather than silently dropping it.

## Verification

Confirm direct dependency success rate and latency have recovered, circuit breakers close, retry volume normalizes, and caller error rates return to baseline. Validate important end-to-end operations rather than relying on a health endpoint alone. Monitor for a renewed failure after traffic controls are removed, then document the dependency impact and follow-up actions.
