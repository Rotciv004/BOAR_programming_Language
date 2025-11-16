window.boarEditor = window.boarEditor || {
	syncScroll: function (gutter, editor) {
		if (!gutter || !editor) return;
		gutter.scrollTop = editor.scrollTop;
	}
};


