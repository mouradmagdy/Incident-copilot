# Incident Response

Use this runbook for any production event that threatens availability, correctness, security, or a customer commitment. The immediate goals are to protect users, establish clear ownership, restore service safely, and preserve enough evidence to learn afterward.

## Initial response

1. Confirm the alert with telemetry or a reproducible customer symptom.
2. Declare an incident, assign a severity, and open a dedicated incident channel.
3. Assign an incident commander. Also identify an investigator and a communications owner when staffing permits.
4. Record the start time, affected services, customer impact, and current hypothesis.
5. Freeze unrelated production changes until the system stabilizes.

The incident commander coordinates decisions and maintains focus; they do not need to perform every investigation. Keep a timestamped timeline of observations, changes, and results. Clearly label facts, hypotheses, and decisions.

## Triage and mitigation

- Assess scope using error rate, latency, traffic, saturation, queue depth, and dependency health.
- Check recent deployments and configuration changes without assuming they are the cause.
- Select the most relevant specialized runbook and follow its investigation steps.
- Prefer reversible, familiar mitigations such as stopping a rollout, disabling a feature flag, shifting traffic, or reducing load.
- State the expected result before each change, make one material change at a time when possible, and verify its effect.
- Escalate early to service or dependency owners when specialized access or knowledge is required.

Never expose secrets or customer data in incident channels. Avoid destructive database actions, untested failovers, and broad restarts without understanding their likely impact.

## Communication

Provide regular updates appropriate to severity. Each update should state customer impact, current status, actions in progress, and the next update time. Communicate uncertainty plainly. Notify stakeholders promptly when impact or estimated recovery changes.

## Recovery and closure

Service is recovered only when the affected customer path works, key metrics remain stable, backlogs are draining, and temporary controls are understood. Monitor through an appropriate stability window before closing the incident.

Afterward, preserve the timeline, identify root and contributing causes, and assign corrective actions with owners and due dates. The review should be blameless and should improve detection, prevention, mitigation, and documentation. Track temporary mitigations until they are safely removed or made permanent.
