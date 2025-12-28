CanvaList = function(sContext)
{
	this.canvas = {};
	this.context = {};
	this.pos = {};
	this.sContext = sContext;
}

CanvaList.prototype.add = function(name, x, y, width, height, flipV, flipH)
{
	var canvas = document.createElement('canvas');
	//document.body.appendChild(canvas)
	canvas.width = width;
	canvas.height = height;
	this.canvas[name] = canvas;
	this.context[name] = canvas.getContext("2d");
	this.pos[name] = {x: x, y: y, v: flipV ? 1 : 0, h: flipH ? 1 : 0};
}

CanvaList.prototype.replace = function(name, x, y, width, height, offX, offY)
{
	var imageData = this.sContext.getImageData(x, y, width, height);
	this.context[name].putImageData(imageData, offX || 0, offY || 0);
}

CanvaList.prototype.update = function()
{
	for (name in this.pos)
	{
		var pos = this.pos[name],
			c = this.canvas[name],
			imageData = this.sContext.getImageData(pos.x, pos.y, c.width, c.height);
		this.context[name].putImageData(imageData, 0, 0);
		if (pos.v)
		{
			var imageData = this.context[name].getImageData(0, 0, c.width, c.height),
				buff = imageData.data,
				step = c.width * 4,
				heightTo = c.height - 1;
			for (var y = 0; y < c.height; y ++)
			{
				var set = step * (heightTo - y);
				for (var x = 0; x < c.width; x ++)
				{
					var off = set + x * 4;
					var t = this.context[name].getImageData(x, y, 1, 1);
					t.data[0] = buff[off]
					t.data[1] = buff[off + 1]
					t.data[2] = buff[off + 2]
					t.data[3] = buff[off + 3]
					this.context[name].putImageData(t, x, y);
				}
			}
		}
		if (pos.h)
		{
			var imageData = this.context[name].getImageData(0, 0, c.width, c.height),
				buff = imageData.data,
				step = c.width * 4 + 4,
				line = c.width * 4 - 4;
			for (var y = 0; y < c.height; y ++)
			{
				var set = y * step;
				for (var x = 0; x < c.width; x ++)
				{
					var off = line - (y * 4 + x * 4) + set;
					var t = this.context[name].getImageData(x, y, 1, 1);
					t.data[0] = buff[off]
					t.data[1] = buff[off + 1]
					t.data[2] = buff[off + 2]
					t.data[3] = buff[off + 3]
					this.context[name].putImageData(t, x, y);
				}
			}
		}
		
	}
}

CanvaList.prototype.write = function()
{
	for (name in this.pos)
	{
		var pos = this.pos[name],
			c = this.canvas[name],
			imageData = this.context[name].getImageData(0, 0, c.width, c.height);
		this.sContext.putImageData(imageData, pos.x, pos.y);
		if (pos.v)
		{
			var imageData = this.sContext.getImageData(pos.x, pos.y, c.width, c.height),
				buff = imageData.data,
				step = c.width * 4,
				heightTo = c.height - 1;
			for (var y = 0; y < c.height; y ++)
			{
				var set = step * (heightTo - y);
				for (var x = 0; x < c.width; x ++)
				{
					var off = set + x * 4;
					var t = this.sContext.getImageData(pos.x + x, pos.y + y, 1, 1);
					t.data[0] = buff[off]
					t.data[1] = buff[off + 1]
					t.data[2] = buff[off + 2]
					t.data[3] = buff[off + 3]
					this.sContext.putImageData(t, pos.x + x, pos.y + y);
				}
			}
		}
		if (pos.h)
		{
			var imageData = this.sContext.getImageData(pos.x, pos.y, c.width, c.height),
				buff = imageData.data,
				step = c.width * 4 + 4,
				line = c.width * 4 - 4;
			for (var y = 0; y < c.height; y ++)
			{
				var set = y * step;
				for (var x = 0; x < c.width; x ++)
				{
					var off = line - (y * 4 + x * 4) + set;
					var t = this.sContext.getImageData(pos.x + x, pos.y + y, 1, 1);
					t.data[0] = buff[off]
					t.data[1] = buff[off + 1]
					t.data[2] = buff[off + 2]
					t.data[3] = buff[off + 3]
					this.sContext.putImageData(t, pos.x + x, pos.y + y);
				}
			}
		}
		
	}
}

/*--*/
SurfaceList = function($el, draw, begin, end)
{
	this.keyDown = '';
	this.lastX = null;
	this.lastY = null;
	var t = this;
	
	$el.mousedown(function(event) {
		t.keyDown = event.which == 1 ? 'left' : 'right';
		begin();
		if (event.target._move)
			event.target._move(event);
	});
	
	var docMouseUp = function(event){
		if (t.keyDown)
		{
			end();
			t.keyDown = '';
		}
		t.lastX = null;
		t.lastY = null;
	}
	$(document).on('mouseup', docMouseUp);
	
	//
	var onMove = function(x, y, shiftKey)
	{
		if (t.keyDown)
		{
			if (x != t.lastX || y != t.lastY)
				draw(this, x, y, t.keyDown, shiftKey)
			t.lastX = x;
			t.lastY = y;
		}
	}
	var onOut = function()
	{
		t.lastX = null;
		t.lastY = null;
	}
	
	this.oList = {};
	this.create = function()
	{
		return new SurfaceMap($el, onMove, onOut)
	}
}

SurfaceList.prototype.get = function(key)
{
	if (!this.oList[key])
		this.oList[key] = this.create()
	
	this.keyDown = '';
	this.lastX = null;
	this.lastY = null;
	
	return this.oList[key];
}

/*--*/
SurfaceMap = function($container, onMove, onOut)
{
	this.list = [];
	var t = this;
	
	this.$el = $('<table><tr><td class="canva-tl"></td><td class="canva-tc"></td><td class="canva-tr"></td></tr><tr><td class="canva-cl"></td><td class="canva-cc"></td><td class="canva-cr"></td></tr><tr><td class="canva-bl"></td><td class="canva-bc"></td><td class="canva-br"></td></tr></table>').appendTo($container);

	this.add = function(canvas, sClass)
	{
		var c = new MyCanvas(canvas, $(sClass, t.$el)[0]);
		c.onMove = onMove;
		c.onOut = onOut;
		t.list.push(c);
		return c;
	}
}

SurfaceMap.prototype.isEmpty = function()
{
	return this.list.length == 0;
}

SurfaceMap.prototype.show = function(renew)
{
	this.$el.css('display', 'block');
	if (renew)
	{
		for (var i = 0; i < this.list.length; i ++)
			this.list[i].update();
	}
}

SurfaceMap.prototype.hide = function()
{
	this.$el.css('display', 'none');
}

SurfaceMap.prototype.clear = function()
{
	for (var i = 0; i < this.list.length; i ++)
		this.list[i].destroy();
	this.list = [];
}

SurfaceMap.prototype.topLeft = function(canvas) { this.add(canvas, '.canva-tl') }
SurfaceMap.prototype.topCenter = function(canvas, link) {
	var c = this.add(canvas, '.canva-tc');
	if (link && this.lastTop)
	{
		this.lastTop.linkRel.bottom = c;
		c.linkRel.top = this.lastTop;
		this.lastTop.context.canvas.style.marginBottom = 0;
		c.context.canvas.style.marginTop = 0;
	}
	this.lastTop = c;
}
SurfaceMap.prototype.topRight = function(canvas) { this.add(canvas, '.canva-tr') }

SurfaceMap.prototype.centerLeft = function(canvas) { this.add(canvas, '.canva-cl') }
SurfaceMap.prototype.centerCenter = function(canvas) { this.add(canvas, '.canva-cc') }
SurfaceMap.prototype.centerRight = function(canvas, link) {
	var c = this.add(canvas, '.canva-cr')
	if (link && this.lastRight)
	{
		this.lastRight.linkRel.right = c;
		c.linkRel.left = this.lastRight;
		this.lastRight.context.canvas.style.marginRight = 0;
		c.context.canvas.style.marginLeft = 0;
	}
	this.lastRight = c;
}

SurfaceMap.prototype.bottomLeft = function(canvas) { this.add(canvas, '.canva-bl') }
SurfaceMap.prototype.bottomCenter = function(canvas, link) {
	var c = this.add(canvas, '.canva-bc');
	if (link && this.lastBottom)
	{
		this.lastBottom.linkRel.bottom = c;
		c.linkRel.top = this.lastBottom;
		this.lastBottom.context.canvas.style.marginBottom = 0;
		c.context.canvas.style.marginTop = 0;
	}
	this.lastBottom = c;
}
SurfaceMap.prototype.bottomRight = function(canvas) { this.add(canvas, '.canva-br') }