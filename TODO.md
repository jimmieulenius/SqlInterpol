* Support RETURNING for UPDATE, DELETE.
* For SQL Server, use $"MERGE INTO {target} WITH (HOLDLOCK) AS target{nl}{usingClause}..." in SqlServerMergeFragment class