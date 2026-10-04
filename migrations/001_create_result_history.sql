CREATE TABLE schema_migrations (
    version INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    applied_at_utc TEXT NOT NULL
);

CREATE TABLE result_submissions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    executed_at_utc TEXT NOT NULL,
    executed_by TEXT NOT NULL CHECK (length(trim(executed_by)) > 0),
    test_target_name TEXT NOT NULL CHECK (length(trim(test_target_name)) > 0)
);

CREATE TABLE test_results (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    submission_id INTEGER NOT NULL,
    repository_root TEXT NOT NULL,
    source_file TEXT NOT NULL,
    test_case_id TEXT NOT NULL,
    result TEXT NOT NULL CHECK (result IN ('pass', 'fail', 'blocked', 'not_applicable')),
    comment TEXT NULL,
    specification_revision TEXT NOT NULL,
    FOREIGN KEY (submission_id) REFERENCES result_submissions(id)
);

CREATE INDEX ix_test_results_case_history
    ON test_results(repository_root, source_file, test_case_id, id DESC);

CREATE INDEX ix_test_results_submission
    ON test_results(submission_id);
