$(function(){
	$('.select-wrap').each(function(){
		var $b = $('<span class="btn"/>').appendTo(this),
			$t = $('<span class="stext"/>').appendTo(this),
		$s = $('select', this).hover(function(){
			$b.mouseover();
		}, function(){
			$b.mouseout();
		}).change(function(){
			$t.html(this.options[this.selectedIndex].innerHTML);
		});
		$t.html($s[0].options[$s[0].selectedIndex].innerHTML);
	})
	
	$('.file-wrap').each(function(){
		var $b = $('<span class="btn">Browse</span>').appendTo(this),
			$t = $('<span class="stext"/>').appendTo(this);
		$('input', this).live('mouseover', function(){
			$b.mouseover();
		}).live('mouseout', function(){
			$b.mouseout();
		}).live('change', function(){
			$t.html(this.value.split(/[\/\\]/).pop());
		});
	})
	
	$('.btn').button();
/*	
	var step = 50;
	$scrl = $('#container').myScroll(step);
	$(window).resize(function(){
		$scrl.myScroll();
	});
	$(document.body).keydown(function(ev){
		var el = $scrl[0],
			code = ev.keyCode ? ev.keyCode : ev.charCode;
		if (['INPUT', 'SELECT', 'TEXTAREA'].indexOf(ev.target.tagName) != -1)
			return;
		switch (code)
		{
			case 38: //up
				el.scrollTop -= step;
				break;
			case 40: //down
				el.scrollTop += step;
				break;
			case 32: //space
				if ('scrlEl' in window)
					return;
				el.scrollTop += el.offsetHeight * (ev.shiftKey ? -1 : 1);
				break;
			case 33: //page up
				el.scrollTop -= el.offsetHeight;
				break;
			case 34: //page down
				el.scrollTop += el.offsetHeight;
				break;
			case 36: //home
				el.scrollTop = 0;
				break;
			case 35: //end
				el.scrollTop = el.scrollHeight;
				break;
		}
		if (code >= 32 && code <= 40)
			$scrl.myScroll();
	})
*/
	//tips
	$('*[title]').each(function(){
		var title = this.title,
			tip = new MyTip(), ti, $el = $(this);
		this.title = '';
		var delay = $el.parents('*[data-tip-delay]').eq(0).attr('data-tip-delay') || .5;
		tip.onHide = function(){ clearTimeout(ti); };
		$el.mouseenter(function(){
			ti = setTimeout(function(){
				tip.show($el, title)
			}, delay * 1000)
		}).mouseleave(function(){
			tip.hide();
		}).data('tip', tip);
	})
	
	if (!Detector.canvas)
	{
		$(document.body).addClass('no-canvas');
		$('#pce img').css('display', 'inline');
	}
	
	$('.embed input, .embed textarea').bind('focus, click', function(){ this.select() })
});

function MyTip()
{
	this.$tip = $('<div class="my-tip"/>');
	this.onHide = function(){};
}

MyTip.prototype.show = function($el, title)
{
	if (MyTip.current)
		MyTip.current.hide();
	var o = $el.offset();
	this.$tip.appendTo(document.body).html(title);
	var left = o.left + ($el.outerWidth() / 2 - this.$tip.outerWidth() / 2),
		top = o.top - this.$tip.outerHeight();
	this.$tip.css({
		'left': left < 0 ? 0 : left,
		'top': top < 0 ? $el.outerHeight() + o.top : top
	});
	MyTip.current = this;
}

MyTip.prototype.hide = function()
{
	this.$tip.detach();
	this.onHide();
}

MyTip.current = null;

function filter(name, value)
{
	var f = document.getElementById('filter');
	f[name].value = value;
	f.submit();
}
