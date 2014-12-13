using Microsoft.VisualStudio.Text.Editor;

namespace MasuqatNet.LanguageExtAdornment
{
	class TextAdornment
	{
		IWpfTextView _view;

		public TextAdornment(IWpfTextView view)
		{
			_view = view;
			view.LayoutChanged += OnLayoutChanged;
		}

		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			using (var writer = new System.IO.StringWriter())
			{
				_view.TextSnapshot.Write(writer);
				//TODO
			}

		}
	}
}
