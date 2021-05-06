package com.microsoft.intune.scepvalidation;

import java.util.Collections;
import java.util.List;
import java.util.concurrent.AbstractExecutorService;
import java.util.concurrent.TimeUnit;

/**
 * This is a non-thread safe ExecutorService implementation that processes all
 * submit() calls immediately rather than running on them on a separate thread.
 * Since ADALClientWrapper doesn't take advantage of concurrency, this should be
 * more efficient, since it doesn't create a new thread with every new
 * ADALClientWrapper instance.
 */
class CurrentThreadExecutor extends AbstractExecutorService {

    boolean isShutdown = false;

    @Override
    public void shutdown() {
        isShutdown = true;
    }

    @Override
    public List<Runnable> shutdownNow() {
        return Collections.emptyList();
    }

    @Override
    public boolean isShutdown() {
        return isShutdown;
    }

    @Override
    public boolean isTerminated() {
        return isShutdown;
    }

    @Override
    public boolean awaitTermination(long timeout, TimeUnit unit) throws InterruptedException {
        return false;
    }

    @Override
    public void execute(Runnable command) {
        command.run();
    }

}
