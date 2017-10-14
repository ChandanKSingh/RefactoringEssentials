using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace RefactoringEssentials.CSharp.CodeRefactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "Split 'if' with '||' condition in two 'if' statements")]
    public class SplitIfWithOrConditionInTwoCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
                return;
            var span = context.Span;
            if (!span.IsEmpty)
                return;
            var cancellationToken = context.CancellationToken;
            if (cancellationToken.IsCancellationRequested)
                return;
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (model.IsFromGeneratedCode(cancellationToken))
                return;
            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);

            var ifNode = token.Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().FirstOrDefault();
            if (ifNode == null)
                return;

            var binOp = token.Parent as BinaryExpressionSyntax;
            if (binOp == null)
                return;

            if (binOp.Ancestors().OfType<BinaryExpressionSyntax>().Any(b => !b.OperatorToken.IsKind(binOp.OperatorToken.Kind())))
                return;

            if (binOp.IsKind(SyntaxKind.LogicalOrExpression))
            {
                context.RegisterRefactoring(
                    CodeActionFactory.Create(
                        span,
                        DiagnosticSeverity.Info,
                        GettextCatalog.GetString("Split into two 'if' statements"),
                        t2 =>
                        {
                            var newElse = ifNode.WithCondition(SplitIfWithAndConditionInTwoCodeRefactoringProvider.GetRightSide(binOp));
                            var newIf = ifNode.WithCondition(SplitIfWithAndConditionInTwoCodeRefactoringProvider.GetLeftSide(binOp)).WithElse(SyntaxFactory.ElseClause(newElse));
                            var newRoot = root.ReplaceNode((SyntaxNode)ifNode, newIf.WithAdditionalAnnotations(Formatter.Annotation));
                            return Task.FromResult(document.WithSyntaxRoot(newRoot));
                        }
                    )
                );
            }
        }

    }
}

