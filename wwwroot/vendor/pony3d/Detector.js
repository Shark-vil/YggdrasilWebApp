var Detector = {
	canvas: (
		function(){
			var elem = document.createElement('canvas');
			return ('getContext' in elem && elem.getContext('2d'));
		}
	)(),
	webgl: (
		function () {
		if (/trident/i.test(navigator.userAgent)) {
			return false;
		}
		try {
			var elem = document.createElement('canvas');
			if (!('getContext' in elem))
				return false;
			var ctx = elem.getContext('experimental-webgl');
			if (!ctx || !ctx.getParameter(ctx.MAX_VERTEX_TEXTURE_IMAGE_UNITS))
				return false;
			return true;
		} catch( e ) { return false; } } )(),
	workers: !! window.Worker,
	fileapi: window.File && window.FileReader && window.FileList && window.Blob,
};
