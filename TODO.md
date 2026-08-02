* Fix SqlTemplate after new simplified syntax
* Add AppendUpsert
* Make sure we have unit tests for templates and Append... methods
* EntityAutoAliasing = true by default - Nope, violates WYSIWYG
* Honor CrossDialectSqlTranspilation in AOT (for example for UPSERT)