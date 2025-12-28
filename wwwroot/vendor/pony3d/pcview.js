(function () {
    function qs(sel) { return document.querySelector(sel); }

    function webglOk() {
        try { return window.Detector && !!Detector.webgl; } catch (_) { return false; }
    }

    function init() {
        // fallback если нет WebGL
        Model.isCanvasRender = webglOk() ? false : true;

        var webglState = document.getElementById('webglState');
        if (webglState) webglState.textContent = Model.isCanvasRender ? 'Canvas mode' : 'WebGL mode';

        var pce = document.getElementById("pce");
        if (!pce) return;

        var fallback = document.getElementById("pceFallback");

        // базова€ поверхность 64x32, как на ponyskincraft
        var cSkin = document.createElement('canvas');
        cSkin.width = 64;
        cSkin.height = 32;
        var cSkinContext = cSkin.getContext("2d", { willReadFrequently: true });
        cSkinContext.clearRect(0, 0, 64, 32);

        var canvalist = new CanvaList(cSkinContext);

        // ====== раскладка кусков как в оригинале ======
        canvalist.add('skinIni', 0, 0, 4, 1);

        canvalist.add('headRight', 0, 8, 8, 8);
        canvalist.add('headFront', 8, 8, 8, 8);
        canvalist.add('headLeft', 16, 8, 8, 8);
        canvalist.add('headRear', 24, 8, 8, 8);
        canvalist.add('headTop', 8, 0, 8, 8);
        canvalist.add('headBottom', 16, 0, 8, 8);

        canvalist.add('earRight', 12, 18, 2, 2);
        canvalist.add('earFront', 14, 18, 2, 2);
        canvalist.add('earLeft', 16, 18, 2, 2);
        canvalist.add('earRear', 18, 18, 2, 2);
        canvalist.add('earTop', 14, 16, 2, 2);
        canvalist.add('earBottom', 16, 16, 2, 2);

        canvalist.add('cornRight', 0, 4, 1, 4);
        canvalist.add('cornFront', 1, 4, 1, 4);
        canvalist.add('cornLeft', 2, 4, 1, 4);
        canvalist.add('cornRear', 3, 4, 1, 4);
        canvalist.add('cornTop', 1, 3, 1, 1);
        canvalist.add('cornBottom', 2, 3, 1, 1);
        canvalist.add('hornGlow', 0, 1, 1, 1);

        canvalist.add('maneRight', 32, 8, 8, 8);
        canvalist.add('maneFront', 40, 8, 8, 8);
        canvalist.add('maneLeft', 48, 8, 8, 8);
        canvalist.add('maneRear', 56, 8, 8, 8);
        canvalist.add('maneTop', 40, 0, 8, 8);
        canvalist.add('maneBottom', 48, 0, 8, 8);

        canvalist.add('neck', 0, 16, 4, 4);

        canvalist.add('bodyFront', 20, 20, 8, 8);
        canvalist.add('bodyTop', 20, 16, 8, 4);
        canvalist.add('bodyBottom', 28, 16, 8, 4, true);
        canvalist.add('bodyRight', 16, 20, 4, 8);
        canvalist.add('bodyLeft', 28, 20, 4, 8);
        canvalist.add('bodyBack', 32, 20, 8, 12, true, true);
        canvalist.add('bodySide', 24, 0, 8, 8);
        canvalist.add('bodyCMark', 4, 0, 4, 8);
        canvalist.add('bodyBelly', 56, 0, 8, 8);
        canvalist.add('bodySex', 36, 16, 8, 4);

        canvalist.add('hindlegRight', 0, 20, 4, 12);
        canvalist.add('hindlegFront', 4, 20, 4, 12);
        canvalist.add('hindlegLeft', 8, 20, 4, 12);
        canvalist.add('hindlegRear', 12, 20, 4, 12);
        canvalist.add('hindlegTop', 4, 16, 4, 4);
        canvalist.add('hindlegBottom', 8, 16, 4, 4, true);

        canvalist.add('forelegRight', 40, 20, 4, 12);
        canvalist.add('forelegFront', 44, 20, 4, 12);
        canvalist.add('forelegLeft', 48, 20, 4, 12);
        canvalist.add('forelegRear', 52, 20, 4, 12);
        canvalist.add('forelegTop', 44, 16, 4, 4);
        canvalist.add('forelegBottom', 48, 16, 4, 4, true);

        canvalist.add('tailFront', 32, 0, 4, 8);
        canvalist.add('tailLeft', 36, 0, 4, 8);

        canvalist.add('wingRight', 56, 18, 2, 12);
        canvalist.add('wingFront', 58, 18, 2, 12);
        canvalist.add('wingLeft', 60, 18, 2, 12);
        canvalist.add('wingRear', 62, 18, 2, 12);
        canvalist.add('wingTop', 58, 16, 2, 2);
        canvalist.add('wingBottom', 60, 16, 2, 2, true);
        // ============================================

        var ini = new Model.Ini(canvalist.context.skinIni);

        var poseSel = qs('#pos select');
        if (poseSel) {
            poseSel.value = 's';
            poseSel.addEventListener('change', function () {
                ini.pos = poseSel.value;
                model.update();
            });
        }

        function size() {
            var r = pce.getBoundingClientRect();
            return {
                w: Math.max(320, Math.floor(r.width)),
                h: Math.max(360, Math.floor(r.height))
            };
        }

        var s = size();
        var model = new Model(ini, canvalist, s.w, s.h, pce);
        model.renderer.domElement.style.display = 'block';

        function redrawFromImage(img) {
            // ѕоддержка:
            // - 64x32 (и HD 2:1) Ч берЄм целиком
            // - 64x64 (и HD 1:1) Ч берЄм верхнюю половину (как классический майн-скин)
            var sw = img.width, sh = img.height;
            cSkinContext.clearRect(0, 0, 64, 32);

            if (sw === sh) {
                // квадрат => верхн€€ половина
                cSkinContext.drawImage(img, 0, 0, sw, sh / 2, 0, 0, 64, 32);
            } else {
                // 2:1 или что-то близкое Ч тупо впихиваем
                cSkinContext.drawImage(img, 0, 0, sw, sh, 0, 0, 64, 32);
            }

            canvalist.update();
            ini.update();
            model.update();

            if (fallback) fallback.style.display = 'none';
        }

        function loadSkin(src) {
            if (!src) return;
            var iSkin = new Image();
            iSkin.onload = function () { redrawFromImage(iSkin); };
            iSkin.onerror = function () {
                // если URL умер Ч просто оставим fallback
                console.warn('Failed to load skin:', src);
            };
            iSkin.src = src;
        }

        // первый запуск Ч из textarea
        var skinData = document.getElementById('skin_data');
        if (skinData) loadSkin((skinData.value || '').trim());

        // публичный API дл€ твоей страницы
        window.Pony3DPreview = {
            setSkin: function (src) { loadSkin(src); },
            setPose: function (p) { ini.pos = p; if (poseSel) poseSel.value = p; model.update(); },
            resize: function () {
                var sz = size();
                model.renderer.setSize(sz.w, sz.h);
                model.camera.aspect = sz.w / sz.h;
                model.camera.updateProjectionMatrix();
                model.update();
            }
        };

        window.addEventListener('resize', function () {
            if (window.Pony3DPreview) window.Pony3DPreview.resize();
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
