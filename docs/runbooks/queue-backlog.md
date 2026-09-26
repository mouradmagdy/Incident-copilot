# Queue Backlog

`ProcessingWorker` consumes asynchronous jobs from the processing queue. A backlog occurs when jobs arrive faster than workers can complete them. Queue depth is the primary signal, but its direction and duration matter: a brief spike that drains is less serious than a smaller backlog that grows continuously.

## Thresholds

- **Normal:** queue depth below 100.
- **Elevated:** queue depth from 100 through 500.
- **Critical:** sustained queue depth above 500.

A critical queue depth is therefore **greater than 500**. Treat an elevated queue as urgent if the oldest-job age approaches a customer-facing service-level objective.

## Common symptoms

- Queue depth and oldest-job age increase over consecutive samples.
- Job processing time rises and completed-job throughput falls below arrival rate.
- `ProcessingWorker` CPU or memory utilization is high.
- Retries, dead-lettered messages, or processing errors increase.
- Request traffic and synchronous APIs may remain relatively normal.

## Investigation

1. Confirm the queue, current depth, growth rate, oldest-job age, and arrival versus completion rate.
2. Check the number and health of `ProcessingWorker` instances.
3. Review worker CPU, memory, garbage collection, logs, and recent deployments.
4. Look for poison messages, retry loops, lock contention, and unusually slow jobs.
5. Check dependencies used during processing, including the database and downstream services.
6. Determine whether a traffic burst, bulk import, or scheduled task created the backlog.

## Mitigation

- Scale healthy workers when jobs are safe to process concurrently and dependencies have capacity.
- Roll back a worker regression or disable a faulty job type.
- Pause or rate-limit nonessential producers to stop continued growth.
- Isolate poison messages and cap retries; do not repeatedly requeue a permanently failing job.
- Restore a slow dependency before adding worker capacity if the dependency is the bottleneck.

## Verification

Verify that completion rate exceeds arrival rate, depth trends downward, oldest-job age falls, and retries and errors return to baseline. Do not close the incident merely because depth drops below 500; confirm it returns below 100 or reaches the agreed normal operating range without harming dependencies.
