﻿using com.github.javaparser.ast.stmt;
using com.github.javaparser.ast.type;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JavaToCSharp.Statements
{
    public class TryStatementVisitor : StatementVisitor<TryStmt>
    {
        public override StatementSyntax Visit(ConversionContext context, TryStmt tryStmt)
        {
            var tryBlock = tryStmt.getTryBlock();
            var tryStatements = tryBlock.getStmts().ToList<Statement>();

            var tryConverted = StatementVisitor.VisitStatements(context, tryStatements);

            var catches = tryStmt.getCatchs().ToList<CatchClause>();

            var trySyn = SyntaxFactory.TryStatement()
                .AddBlockStatements(tryConverted.ToArray());

            if (catches != null)
            {
                foreach (var ctch in catches)
                {
                    var paramType = ctch.getParam().getType();
                    if (paramType is UnionType)
                    {
                        var nodes = paramType.getChildrenNodes();
                        foreach (var node in nodes.ToList<ReferenceType>())
                        {
                            var referenceTypeName = node.getType().ToString();
                            trySyn = addCatches(context, ctch, referenceTypeName, trySyn);
                        }
                    }
                    else
                    {
                        var referenceType = (ReferenceType)ctch.getParam().getType();
                        trySyn = addCatches(context, ctch, referenceType.getType().ToString(), trySyn);
                    }
                }
            }

            var finallyBlock = tryStmt.getFinallyBlock();

            if (finallyBlock != null)
            {
                var finallyStatements = finallyBlock.getStmts().ToList<Statement>();
                var finallyConverted = StatementVisitor.VisitStatements(context, finallyStatements);
                var finallyBlockSyntax = SyntaxFactory.Block(finallyConverted);

                trySyn = trySyn.WithFinally(SyntaxFactory.FinallyClause(finallyBlockSyntax));
            }

            return trySyn;
        }

        private static TryStatementSyntax addCatches(ConversionContext context, CatchClause ctch,
            string typeName, TryStatementSyntax trySyn)
        {
            var block = ctch.getCatchBlock();
            var catchStatements = block.getStmts().ToList<Statement>();
            var catchConverted = StatementVisitor.VisitStatements(context, catchStatements);
            var catchBlockSyntax = SyntaxFactory.Block(catchConverted);

            var type = TypeHelper.ConvertType(typeName);

            trySyn = trySyn.AddCatches(
                SyntaxFactory.CatchClause(
                    SyntaxFactory.CatchDeclaration(
                        SyntaxFactory.ParseTypeName(type),
                        SyntaxFactory.ParseToken(ctch.getParam().getId().toString())
                    ),
                    filter: null,
                    block: catchBlockSyntax
                )
            );
            return trySyn;
        }
    }
}
