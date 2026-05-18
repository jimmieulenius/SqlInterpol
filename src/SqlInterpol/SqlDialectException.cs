using System;

namespace SqlInterpol;

public class SqlDialectException(string message) : Exception(message)
{
}