(function () {

	var bwfc = {

		SetPageTitle: function (title) {

			document.title = title;

		},

		GetPageTitle: function () {
			return document.title;
		}
		
	};

	window.BlazorWebFormsComponents = bwfc;

})();
