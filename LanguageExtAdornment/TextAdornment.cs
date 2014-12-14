using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace MasuqatNet.LanguageExtAdornment
{
	class TextAdornment
	{
		IWpfTextView _view;
		IAdornmentLayer _layer;

		Brush _preludeBrush;
		Pen _preludePen;

		Brush _wordBrush;
		Pen _wordPen;

		ISet<string> _preludeMethodNameSet;

		public TextAdornment(IWpfTextView view)
		{
			//演算子風のワードをハイライトする
			_preludeMethodNameSet = new HashSet<string>(typeof(Prelude).GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name).Distinct().Where(n => n.All(c => char.IsLower(c))));

			_view = view;
			_layer = view.GetAdornmentLayer(nameof(LanguageExtAdornment));

			_preludeBrush = new SolidColorBrush(Colors.Coral) { Opacity = 0.2 };
			_preludeBrush.Freeze();
			_preludePen = new Pen();
			_preludePen.Freeze();

			_wordBrush = new SolidColorBrush(Colors.Navy) { Opacity = 0.2 };
			_wordBrush.Freeze();
			_wordPen = new Pen();
			_wordPen.Freeze();

			view.LayoutChanged += OnLayoutChanged;
		}

		private async void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			//TODO: 非同期処理化
			var builder = new StringBuilder();
			using (var writer = new StringWriter(builder))
			{
				_view.TextSnapshot.Write(writer);
			}
			var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(builder.ToString()));

			var root = await syntaxTree.GetRootAsync();
			var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
			if (!usings.Any())
			{
				return;
			}

			//using LanguageExt.Prelude と using Prelude を探す
			var identifierNames = usings.Select(u => u.ChildNodes()).Where(ns => ns.Any(nss => nss.IsKind(SyntaxKind.IdentifierName))).Select(ns => ns.Single());
			var qualifiedNames = usings.Select(u => u.ChildNodes()).Where(ns => !ns.Any(nss => nss.IsKind(SyntaxKind.NameEquals))).Where(ns => ns.Any(nss => nss.IsKind(SyntaxKind.QualifiedName))).Select(ns => ns.Single());
			var twoIdentifierNames = qualifiedNames.Where(q => !q.ChildNodes().Any(qs => qs.IsKind(SyntaxKind.QualifiedName)));

			var shortPrelude = identifierNames.Where(i => i.GetFirstToken().Text == nameof(Prelude));
			var longPrelude = twoIdentifierNames.Where(t => t.GetFirstToken().Text == nameof(LanguageExt) && t.GetLastToken().Text == nameof(Prelude));
			var preludeUsings = Enumerable.Union(shortPrelude, longPrelude);
			if (!preludeUsings.Any())
			{
				return;
			}

			//ここに到達する場合、LanguageExt.Preludeを使っている
			_layer.RemoveAllAdornments();
			foreach (var aPrelude in preludeUsings.Select(p => p.Parent))
			{
				Draw(aPrelude.Span, _preludeBrush, _preludePen);

				//TODO: ローカル変数名tupleなどのフィルタリング
				var invocations = aPrelude.Parent.DescendantNodes().OfType<InvocationExpressionSyntax>();
				var directMethods = invocations.Where(i => !i.ChildNodes().Any(@is => @is.IsKind(SyntaxKind.SimpleMemberAccessExpression)));
				var preludeMethodNames = directMethods.Select(d => d.ChildNodes().Single(ds => ds.IsKind(SyntaxKind.IdentifierName))).Where(d => _preludeMethodNameSet.Contains(d.GetFirstToken().Text));
				foreach (var aMethodName in preludeMethodNames)
				{
					Draw(aMethodName.Span, _wordBrush, _wordPen);
				}
			}
		}

		private void Draw(TextSpan syntaxSpan, Brush brush, Pen pen)
		{
			//SDKのTexAdornmentサンプルを抜き出し
			var span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(syntaxSpan.Start, syntaxSpan.End));
			var geometry = _view.TextViewLines.GetMarkerGeometry(span);
			if (geometry != null)
			{
				GeometryDrawing drawing = new GeometryDrawing(brush, pen, geometry);
				drawing.Freeze();

				DrawingImage drawingImage = new DrawingImage(drawing);
				drawingImage.Freeze();

				Image image = new Image();
				image.Source = drawingImage;

				//Align the image with the top of the bounds of the text geometry
				Canvas.SetLeft(image, geometry.Bounds.Left);
				Canvas.SetTop(image, geometry.Bounds.Top);

				_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
			}
		}
	}
}
