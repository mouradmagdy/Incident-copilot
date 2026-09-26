# Deployment Regression

A deployment regression is a production fault introduced by a new application version, configuration change, feature flag, infrastructure release, or database migration. The strongest initial indicator is a clear change in service health shortly after a release, but timing alone does not prove causation.

## Common symptoms

- Error rate or latency rises soon after deployment.
- Only instances running the new version are unhealthy.
- New exception messages, crashes, failed health checks, or resource growth appear.
- A particular endpoint, tenant, region, or feature-flag cohort is affected.
- Queue depth or database load changes despite stable request traffic.

## Investigation

1. Record the incident start time and compare it with all recent deployments and configuration changes.
2. Split metrics and logs by application version, instance, region, and feature-flag state.
3. Review the release diff, migration status, dependency versions, and environment configuration.
4. Compare a known-good request against the failing version using traces and structured logs.
5. Check whether rollback is compatible with database and message-schema changes.
6. Rule out coincident traffic spikes or dependency failures before declaring the release causal.

## Mitigation

- Stop the rollout to limit exposure.
- Roll back to the last known-good version when rollback is safe.
- Disable the responsible feature flag or configuration change when that is faster and lower risk.
- Route traffic away from unhealthy instances or regions.
- If a database migration is not backward-compatible, use the documented forward-fix or recovery plan; do not blindly deploy old code against an incompatible schema.
- Preserve logs, traces, release identifiers, and the exact failing input for later analysis.

## Verification

Confirm that healthy capacity is restored and error rate, latency, saturation, and queue depth return to their pre-deployment baselines. Exercise the affected customer path and watch a complete deployment or traffic cycle. Ensure no failing version remains in service and no partial migration or configuration drift remains.

Document the causal change, why release checks missed it, and the permanent prevention work. Typical follow-ups include a regression test, stronger canary criteria, schema compatibility checks, or improved rollback automation.
