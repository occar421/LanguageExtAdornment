using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MasuqatNet.LanguageExtAdornment
{
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	internal sealed class Factory : IWpfTextViewCreationListener
	{
		[Export(typeof(AdornmentLayerDefinition))]
		[Name("LanguageExtAdornment")]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
		public AdornmentLayerDefinition editorAdornmentLayer = null;

		public void TextViewCreated(IWpfTextView textView)
		{
			new TextAdornment(textView);
		}
	}
}
