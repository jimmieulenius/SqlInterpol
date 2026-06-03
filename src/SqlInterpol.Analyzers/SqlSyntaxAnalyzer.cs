// using System;
// using System.Text;
// using System.Collections.Immutable;
// using Antlr4.Runtime;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Diagnostics;
// using Microsoft.CodeAnalysis.Text;
// using SqlInterpol.Analyzers.Grammars;
// using System.IO;

// namespace SqlInterpol.Analyzers;

// [DiagnosticAnalyzer(LanguageNames.CSharp)]
// public class SqlSyntaxAnalyzer : DiagnosticAnalyzer
// {
//     private static readonly DiagnosticDescriptor SyntaxRule = new(
//         id: "SQLI004",
//         title: "Invalid SQL Syntax",
//         messageFormat: "SQL Parse Error: {0}",
//         category: "Correctness",
//         defaultSeverity: DiagnosticSeverity.Error, 
//         isEnabledByDefault: true);

//     public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
//         ImmutableArray.Create(SyntaxRule);

//     public override void Initialize(AnalysisContext context)
//     {
//         context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
//         context.EnableConcurrentExecution();
//         context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
//     }

//     private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
//     {
//         var invocation = (InvocationExpressionSyntax)context.Node;
        
//         string methodName = "";
//         if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
//             methodName = memberAccess.Name.Identifier.Text;
//         else if (invocation.Expression is IdentifierNameSyntax identifierName)
//             methodName = identifierName.Identifier.Text;

//         if (methodName != "Append" && methodName != "AppendLine") return;
//         if (invocation.ArgumentList.Arguments.Count == 0) return;

//         var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
//         bool isTargetMethod = false;

//         // Lenient target validation to survive generic lambda scopes
//         if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
//         {
//             var containingType = methodSymbol.ContainingType?.ToDisplayString() ?? "";
//             var returnType = methodSymbol.ReturnType?.ToDisplayString() ?? "";
//             var receiverType = "";

//             if (invocation.Expression is MemberAccessExpressionSyntax maes)
//             {
//                 var typeInfo = context.SemanticModel.GetTypeInfo(maes.Expression);
//                 receiverType = typeInfo.Type?.ToDisplayString() ?? "";
//             }

//             if (containingType.Contains("SqlBuilder") || containingType.Contains("SqlInterpol") ||
//                 returnType.Contains("SqlBuilder") || returnType.Contains("SqlInterpol") ||
//                 receiverType.Contains("SqlBuilder") || receiverType.Contains("SqlInterpol"))
//             {
//                 isTargetMethod = true;
//             }
//             else if (string.IsNullOrEmpty(receiverType) || receiverType == "?" || receiverType.Contains("Error"))
//             {
//                 isTargetMethod = true; 
//             }
//         }
//         else
//         {
//             isTargetMethod = true; 
//         }

//         if (!isTargetMethod) return;

//         var firstArg = invocation.ArgumentList.Arguments[0].Expression;
//         if (firstArg is not InterpolatedStringExpressionSyntax interpolatedString) return;

//         var sb = new StringBuilder();
//         foreach (var content in interpolatedString.Contents)
//         {
//             if (content is InterpolatedStringTextSyntax textSyntax)
//                 sb.Append(textSyntax.TextToken.Text); // Retains physical whitespace
//             else if (content is InterpolationSyntax)
//                 sb.Append(" __dummy__ "); // Exactly 11 characters
//         }

//         var sqlToParse = sb.ToString();
//         if (string.IsNullOrWhiteSpace(sqlToParse.Replace("__dummy__", ""))) return;

//         var errorListener = new SqlErrorListener(context, interpolatedString, sqlToParse);

//         try
//         {
//             var charStream = CharStreams.fromString(sqlToParse);
//             var lexer = new SqlInterpolLexer(charStream);
//             var tokenStream = new CommonTokenStream(lexer);
//             var parser = new SqlInterpolParser(tokenStream);
            
//             lexer.RemoveErrorListeners();
//             lexer.AddErrorListener(errorListener);
            
//             parser.RemoveErrorListeners();
//             parser.AddErrorListener(errorListener);

//             // Execute the catch-all fragment rule we added to the grammar
//             parser.sql_fragment();

//             // Catch silent parsing dropouts (e.g. completely unrecoverable statements)
//             if (parser.NumberOfSyntaxErrors == 0 && tokenStream.LA(1) != Antlr4.Runtime.TokenConstants.EOF)
//             {
//                 var badToken = tokenStream.LT(1);
//                 errorListener.SyntaxError(
//                     null, 
//                     parser, 
//                     badToken, 
//                     badToken.Line, 
//                     badToken.Column, 
//                     $"Unrecognized SQL syntax starting at: '{badToken.Text}'", 
//                     null);
//             }
//         }
//         catch (Exception ex)
//         {
//             var diagnostic = Diagnostic.Create(SyntaxRule, interpolatedString.GetLocation(), $"Parser Exception: {ex.Message}");
//             context.ReportDiagnostic(diagnostic);
//         }
//     }

//     private sealed class SqlErrorListener : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
//     {
//         private readonly SyntaxNodeAnalysisContext _roslynContext;
//         private readonly InterpolatedStringExpressionSyntax _interpolatedString;
//         private readonly string _sqlToParse;
//         private bool _errorReported = false;

//         public SqlErrorListener(SyntaxNodeAnalysisContext roslynContext, InterpolatedStringExpressionSyntax interpolatedString, string sqlToParse)
//         {
//             _roslynContext = roslynContext;
//             _interpolatedString = interpolatedString;
//             _sqlToParse = sqlToParse;
//         }

//         // Lexer Errors
//         public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
//         {
//             try { ReportDiagnostic(msg, charPositionInLine, charPositionInLine + 1); }
//             catch { ReportFallback(msg); }
//         }

//         // Parser Errors
//         public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
//         {
//             if (offendingSymbol != null)
//             {
//                 // 1. Ignore genuinely incomplete dynamic queries (e.g., "WHERE \n")
//                 if (offendingSymbol.Type == Antlr4.Runtime.TokenConstants.EOF)
//                 {
//                     return; 
//                 }

//                 // 2. Ignore continuation operators ONLY if they happen at the very start of the chunk
//                 string text = offendingSymbol.Text ?? "";
//                 if (text.Equals("AND", StringComparison.OrdinalIgnoreCase) || 
//                     text.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
//                     text.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
//                     text.Equals(",", StringComparison.OrdinalIgnoreCase))
//                 {
//                     int start = Math.Max(0, offendingSymbol.StartIndex);
//                     if (start <= _sqlToParse.Length)
//                     {
//                         string prefix = _sqlToParse.Substring(0, start);
//                         if (string.IsNullOrWhiteSpace(prefix)) return; 
//                     }
//                 }
//             }

//             try { ReportDiagnostic(msg, offendingSymbol?.StartIndex ?? -1, offendingSymbol?.StopIndex ?? -1); }
//             catch { ReportFallback(msg); }
//         }

//         private void ReportFallback(string msg)
//         {
//             if (_errorReported) return;
//             _errorReported = true;
//             var diagnostic = Diagnostic.Create(SyntaxRule, _interpolatedString.GetLocation(), msg);
//             _roslynContext.ReportDiagnostic(diagnostic);
//         }

//         private void ReportDiagnostic(string msg, int startIndex, int stopIndex)
//         {
//             if (_errorReported) return;
//             _errorReported = true;

//             Location location = _interpolatedString.GetLocation(); // Fallback location covers the whole string

//             try
//             {
//                 if (startIndex >= 0 && stopIndex >= startIndex)
//                 {
//                     int sqlOffset = 0;
//                     foreach (var content in _interpolatedString.Contents)
//                     {
//                         int chunkLength = 0;
//                         TextSpan chunkSpan = default;

//                         if (content is InterpolatedStringTextSyntax textSyntax)
//                         {
//                             chunkLength = textSyntax.TextToken.Text.Length;
//                             chunkSpan = textSyntax.Span;
//                         }
//                         else if (content is InterpolationSyntax interpSyntax)
//                         {
//                             chunkLength = 11; // Matches " __dummy__ "
//                             chunkSpan = interpSyntax.Span;
//                         }
//                         else continue;

//                         // Check if the ANTLR error index falls inside this specific Roslyn chunk
//                         if (startIndex >= sqlOffset && startIndex < sqlOffset + chunkLength)
//                         {
//                             if (content is InterpolatedStringTextSyntax)
//                             {
//                                 int tokenStartInText = startIndex - sqlOffset;
//                                 int tokenStopInText = Math.Min(stopIndex - sqlOffset, chunkLength - 1);
                                
//                                 int start = chunkSpan.Start + tokenStartInText;
//                                 int length = tokenStopInText - tokenStartInText + 1;
                                
//                                 // Make absolutely sure the span doesn't bleed out of bounds
//                                 if (start >= chunkSpan.Start && start + length <= chunkSpan.End)
//                                 {
//                                     location = Location.Create(_roslynContext.Node.SyntaxTree, new TextSpan(start, length));
//                                 }
//                             }
//                             else 
//                             {
//                                 // If the error occurred inside an interpolation variable (e.g. {{p}}), highlight the whole variable
//                                 location = Location.Create(_roslynContext.Node.SyntaxTree, chunkSpan);
//                             }
//                             break;
//                         }
//                         sqlOffset += chunkLength;
//                     }
//                 }
//             }
//             catch 
//             {
//                 // If the math fails due to complex string formatting, fail silently and keep the fallback location
//             }

//             var diagnostic = Diagnostic.Create(SyntaxRule, location, msg);
//             _roslynContext.ReportDiagnostic(diagnostic);
//         }
//     }
// }