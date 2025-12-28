Model = function(ini, canvalist, offsetWidth, offsetHeight, container)
{
	this.renderer = Model.isCanvasRender ? new THREE.CanvasRenderer() : new THREE.WebGLRenderer();
	this.renderer.setSize(offsetWidth, offsetHeight);
	this.ini = ini;
	
	this.camera = new THREE.PerspectiveCamera(75, offsetWidth / offsetHeight, 1, 1000);
	this.camera.position.z = 40;
	
	this.scene = new THREE.Scene();
	
	var lightStrong = 0x222222;
	var light = new THREE.AmbientLight(0xffffff - lightStrong);
	this.scene.add(light);
	
	var plight = new THREE.PointLight(lightStrong, 1, 0);
	plight.position = this.camera.position;
	this.scene.add(plight);

	var objects = new THREE.Object3D();
	this.scene.add(objects);
	objects.rotation.y = -0.8;

	this.pony = new Model.Pony(ini, canvalist);
	
	objects.add(this.pony.object3D);
	
	// items
	var elCanvas = document.createElement('canvas');
	elCanvas.width = 16;
	elCanvas.height = 96;
	var itemsContext = elCanvas.getContext("2d");
	var elItemsImg = new Image();
	elItemsImg.onload = function()
	{
		itemsContext.drawImage(elItemsImg, 0, 0);
	}
	elItemsImg.src = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAABgCAMAAAAjDwKSAAAAQlBMVEUAAAAIJSAMNzAOPzYVY1Ube2seincglYEnsporx6wszbEw270z68tK7dGM9OKi9ufR+vP///+JZydoTh5JNhUoHgsHRvipAAAAAXRSTlMAQObYZgAAARpJREFUeNrFVNGSgyAMJKZKpHJghf//1QsqStK7TtvrzO3bbgJx16gxLRBR8mlExSd8jwOoOlxA9cMA8jwL8r6iQMvTAsOlUdIt86RDAVjyMXw/LwxyRQkv4T6viZR/ag0ULh0lzW9ZcyP4ZkjUze/1fFdXnJVsPgFEGUaxSiOmRXQQ6ecZRWDl+andudV/07L6a4TNDx5Hdv9YQzz84nP5mSW/n4DRy6A+HuGscKImnzWygbK4MrGi3j+Rzgcf7Mdr+/K5PH74lYxKQbkfRXpmP/Avfv4rj44h+MzoWh5CiNfu5DGE2ftGCHOM0deWwr8Y1p9C3IS9ha9wq9D3VfDeMpyz9YzvHaN3dUx33QR7zrVFsJ20Is08wDdqbRIfHQCSxgAAAABJRU5ErkJggg==';
	this.item = new Model.Item(canvalist.context.hornGlow, itemsContext, ini);
	objects.add(this.item.object3D);
	//

	if (container)
	{
		container.appendChild(this.renderer.domElement);

		var t = this;
		var controls = new THREE.OrbitControls(this.camera, container);
		controls.addEventListener('change', function(){
			t.renderer.render(t.scene, t.camera);
			//console.log(t.camera.position);
		});
		controls.userRotateSpeed = 2;
		
		// panorama
		var clock = new THREE.Clock(),
			duration = 200,
			pos = 0;
			width = -2361,
			v = width / duration;
		
		function animate()
		{
			requestAnimationFrame(animate);
			controls.update();
			
			pos += v * clock.getDelta();
			if (pos <= width)
				pos = 0;
			container.style.backgroundPosition = pos + 'px 0';
		}
		
		animate();
	}
	this.lastPos = this.ini.pos;
}

Model.prototype.update = function(obj)
{
	(obj || this.pony).update();
	
	if (this.lastPos != this.ini.pos && this.ini.pos == 'h')
		this.item.create();

	this.item.update();
	this.lastPos = this.ini.pos;
	
	this.renderer.render(this.scene, this.camera);
}

Model.isCanvasRender = true;

// basic object 
Model.Object = function()
{
	this.object3D = null;
	this.children = [];
	this.visible = true;
	this.display = true;
}

Model.Object.prototype.init = function() {}

Model.Object.prototype.update = function()
{
	var i, len;
	len = this.children.length;
	for (i = 0; i < len; i++)
		this.children[i].update();
}

Model.Object.prototype.setPosition = function(x, y, z)
{
	if (this.object3D)
		this.object3D.position.set(x, y, z);
}

Model.Object.prototype.setRotation = function(x, y, z)
{
	if (this.object3D)
		this.object3D.rotation.set(x, y, z);
}

Model.Object.prototype.setScale = function(x, y, z)
{
	if (this.object3D)
	{
		if (arguments.length == 1)
			y = z = x;
		this.object3D.scale.set(x, y, z);
	}
}

Model.Object.prototype.setVisible = function(visible)
{
	function setVisible(obj, visible)
	{
		obj.visible = visible;
		var i, len = obj.children.length;
		for (i = 0; i < len; i++)
			setVisible(obj.children[i], visible);
	}
	if (this.object3D)
		setVisible(this.object3D, this.display && visible);
	this.visible = visible;
}

Model.Object.prototype.setDisplay = function(display)
{
	function setDisplay(obj)
	{
		var i, len = obj.children.length;
		obj.display = display;
		for (i = 0; i < len; i++)
			setDisplay(obj.children[i]);
	}
	setDisplay(this);
	this.setVisible(this.visible);
}

Model.Object.prototype.addChild = function(child)
{
	this.children.push(child);
	if (child.object3D)
		this.object3D.add(child.object3D);
}

Model.Object.prototype.removeChild = function(child)
{
	var index = this.children.indexOf(child);
	if (index != -1)
	{
		this.children.splice(index, 1);
		if (child.object3D)
			this.object3D.remove(child.object3D);
	}
}

// readIni
Model.Ini = function(context)
{
	this.context = context;
	this.type = 'e';
	this.tail = '4';
	this.size = 'n';
	this.snout = 'r';
	
	this.pos = 's';
}

Model.Ini.prototype.clone = function()
{
	var ini = new Model.Ini(this.context);
	ini.type = this.type;
	ini.tail = this.tail;
	ini.size = this.size;
	ini.snout = this.snout;
	return ini;
}

Model.Ini.prototype.update = function()
{
	var data = this.context.getImageData(0, 0, 4, 1).data;
	function getPixel(i)
	{
		i = i * 4;
		return (data[i] << 16) + (data[i + 1] << 8) + data[i + 2];
	}
	
	// type
	switch (getPixel(0))
	{
		case 0xd19fe4: this.type = 'u'; break;
		case 0x88caf0: this.type = 'p'; break;
		case 0xf9b131: this.type = 'e'; break;
		case 0xfef9fc: this.type = 'a'; break;
	}
	
	// tail
	switch (getPixel(1))
	{
		case 0x425844: this.tail = '0'; break;
		case 0x468e88: this.tail = '1'; break;
		case 0x534b76: this.tail = '2'; break;
		case 0x8a6b7f: this.tail = '3'; break;
		case 0x000000: this.tail = '4'; break;
	}

	// snout
	switch (getPixel(2))
	{
		case 0x000000: this.snout = 'r'; break;
		case 0xffffff: this.snout = 's'; break;
	}
	
	// size
	switch (getPixel(3))
	{
		case 0x000000: this.size = 'n'; break;
		case 0xffbe53: this.size = 'f'; break;
		case 0x2a3c78: this.size = 'a'; break;
		case 0xce3254: this.size = 'l'; break;
	}
	
	this.onupdate();
}

Model.Ini.prototype.onupdate = function() { }
Model.Ini.prototype.write = function()
{
	var id = this.context.getImageData(0, 0, 4, 1),
		data = id.data;
	function setPixel(i, bigint)
	{
		i = i * 4;
		data[i] = (bigint >> 16) & 255;
		data[i + 1] = (bigint >> 8) & 255;
		data[i + 2] = bigint & 255;
		data[i + 3] = bigint ? 255 : 0;
	}
	
	// type
	switch (this.type)
	{
		case 'u': setPixel(0, 0xd19fe4); break;
		case 'p': setPixel(0, 0x88caf0); break;
		case 'e': setPixel(0, 0xf9b131); break;
		case 'a': setPixel(0, 0xfef9fc); break;
	}
	
	// tail
	switch (this.tail)
	{
		case '0': setPixel(1, 0x425844); break;
		case '1': setPixel(1, 0x468e88); break;
		case '2': setPixel(1, 0x534b76); break;
		case '3': setPixel(1, 0x8a6b7f); break;
		case '4': setPixel(1, 0x000000); break;
	}

	// snout
	switch (this.snout)
	{
		case 'r': setPixel(2, 0x000000); break;
		case 's': setPixel(2, 0xffffff); break;
	}
	
	// size
	switch (this.size)
	{
		case 'n': setPixel(3, 0x000000); break;
		case 'f': setPixel(3, 0xffbe53); break;
		case 'a': setPixel(3, 0x2a3c78); break;
		case 'l': setPixel(3, 0xce3254); break;
	}
	this.context.putImageData(id, 0, 0)
}

// pony
Model.Pony = function(ini, canvalist)
{
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();
	
	this.ini = ini;
	
	var c = new Model.PonyHead(ini, canvalist);
	this.addChild(c);
	this.head = c;

	var c = new Model.PonyNeck(ini, canvalist);
	this.addChild(c);
	this.neck = c;

	var c = new Model.PonyBody(ini, canvalist);
	this.addChild(c);
	this.body = c;

	var c = new Model.PonyLegs(ini, canvalist);
	this.addChild(c);
	this.legs = c;

	var c = new Model.PonyTail(ini, canvalist);
	this.addChild(c);
	this.tail = c;

	var c = new Model.PonyWings(ini, canvalist);
	this.addChild(c);
	this.wings = c;
}
Model.Pony.prototype = new Model.Object()

// pony head
Model.PonyHead = function(ini, canvalist)
{
	this.ini = ini;
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();

	//base
	this.base = new Model.Object();
	this.base.object3D = new THREE.Object3D();
	this.addChild(this.base);
	
	var c = new Model.Cube(8, 8, 8);
	c.addFront(new Texture(canvalist.canvas.headFront))
		.addRear(new Texture(canvalist.canvas.headRear))
		.addRight(new Texture(canvalist.canvas.headRight))
		.addLeft(new Texture(canvalist.canvas.headLeft))
		.addTop(new Texture(canvalist.canvas.headTop))
		.addBottom(new Texture(canvalist.canvas.headBottom));
	this.base.addChild(c);
	
	//face
	c = new Model.Cube(4, 2, 1);
	c.addFront(new TextureSub(canvalist.canvas.headFront, 2, 6, 4, 2))
		.addRight(new TextureSub(canvalist.canvas.headFront, 2, 6, 1, 2))
		.addLeft(new TextureSub(canvalist.canvas.headFront, 5, 6, 1, 2))
		.addTop(new TextureSub(canvalist.canvas.headFront, 2, 6, 4, 1))
		.addBottom(new TextureSub(canvalist.canvas.headFront, 2, 7, 4, 1));
	c.setPosition(0, -3, 4.5)
	this.base.addChild(c);
	this.faceB = c;
	
	c = new Model.Cube(2, 1, 1);
	c.addFront(new TextureSub(canvalist.canvas.headFront, 3, 5, 2, 1))
		.addRight(new TextureSub(canvalist.canvas.headFront, 3, 5, 1, 1))
		.addLeft(new TextureSub(canvalist.canvas.headFront, 4, 5, 1, 1))
		.addTop(new TextureSub(canvalist.canvas.headFront, 3, 5, 2, 1));
	c.setPosition(0, -1.5, 4.5);
	this.base.addChild(c);
	this.faceR = c;
	
	c = new Model.Cube(4, 1, 1);
	c.addFront(new TextureSub(canvalist.canvas.headFront, 2, 5, 4, 1))
		.addRight(new TextureSub(canvalist.canvas.headFront, 2, 5, 1, 1))
		.addLeft(new TextureSub(canvalist.canvas.headFront, 5, 5, 1, 1))
		.addTop(new TextureSub(canvalist.canvas.headFront, 2, 5, 4, 1));
	c.setPosition(0, -1.5, 4.5);
	this.base.addChild(c);
	this.faceS = c;
	
	//ears
	this.ears = new Model.Object();
	this.ears.object3D = new THREE.Object3D();
	this.ears.setPosition(0, 5, 0);
	this.addChild(this.ears);
	
	c = new Model.Cube(2, 2, 2);
	c.basic = false;
	c.addFront(new Texture(canvalist.canvas.earFront))
		.addRear(new Texture(canvalist.canvas.earRear))
		.addRight(new Texture(canvalist.canvas.earRight))
		.addLeft(new Texture(canvalist.canvas.earLeft))
		.addTop(new Texture(canvalist.canvas.earTop));
	c.setPosition(-3, 0, -2);
	this.ears.addChild(c);
	this.ear = c;
	
	c = new Model.Cube(2, 2, 2);
	c.basic = false;
	c.addFront(new Texture(canvalist.canvas.earFront))
		.addRear(new Texture(canvalist.canvas.earRear))
		.addRight(new Texture(canvalist.canvas.earRight))
		.addLeft(new Texture(canvalist.canvas.earLeft))
		.addTop(new Texture(canvalist.canvas.earTop));
	c.toleft();
	c.setPosition(3, 0, -2);
	this.ears.addChild(c);
	
	//mane
	var c = new Model.Cube(8, 8, 8);
	c.basic = false;
	c.addFront(new Texture(canvalist.canvas.maneFront))
		.addRear(new Texture(canvalist.canvas.maneRear))
		.addRight(new Texture(canvalist.canvas.maneRight))
		.addLeft(new Texture(canvalist.canvas.maneLeft))
		.addTop(new Texture(canvalist.canvas.maneTop))
		.addBottom(new Texture(canvalist.canvas.maneBottom));
	c.setScale(1.11);
	this.addChild(c);
	this.mane = c;
	
	//corn
	this.corn = new Model.Object();
	this.corn.object3D = new THREE.Object3D();
	this.addChild(this.corn);
	
	var c = new Model.Cube(1, 4, 1);
	c.basic = false;
	c.addFront(new Texture(canvalist.canvas.cornFront))
		.addRear(new Texture(canvalist.canvas.cornRear))
		.addRight(new Texture(canvalist.canvas.cornRight))
		.addLeft(new Texture(canvalist.canvas.cornLeft))
		.addTop(new Texture(canvalist.canvas.cornTop));
	this.corn.addChild(c);
	
	this.glow = new Model.Glow(canvalist.context.hornGlow);
	this.corn.addChild(this.glow);

	this.corn.setPosition(0, 5.2, 4.7);
	this.corn.setRotation(0.6, 0, 0);
	//
	
	this.setPosition(0, 10, 9);
}
Model.PonyHead.prototype = new Model.Object()
Model.PonyHead.prototype.update = function()
{
	var pos = {
		n: {s: [0, 10, 9], b: [0, 6, 10.5], r: [-1.5, 7.6, 4.5]},
		f: {s: [0, 6.4, 5.5], b: [0, 5, 6.5], r: [-1.5, 6.6, 2.8]},
		l: {s: [0, 11, 10.2], b: [0, 7, 12.7], r: [-2.5, 8.4, 6.2]}
	};

	this.corn.setVisible(this.ini.type == 'u' || this.ini.type == 'a');
	
	this.faceS.setVisible(this.ini.snout == 's');
	this.faceR.setVisible(this.ini.snout == 'r');

	//position
	switch (this.ini.pos)
	{
		case 's':
		case 'h':
			this.setRotation(0, 0, 0);
			break;
		case 'b':
			this.setRotation(.2, 0, 0);
			break;
		case 'r':
			this.setRotation(.8, -1.42, .8)
			break;
	}
	this.glow.setVisible(this.ini.pos == 'h' && (this.ini.type == 'u' || this.ini.type == 'a'))
	
	//size
	var k = this.ini.size,
		p = pos[k == 'a' ? 'n' : k][this.ini.pos == 'h' ? 's' : this.ini.pos];
	this.setPosition(p[0], p[1], p[2]);

	Model.Object.prototype.update.call(this);
}

// pony neck
Model.PonyNeck = function(ini, canvalist)
{
	this.ini = ini;
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();

	var c = new Model.Cube(4, 4, 4);
	c.basic = false;
	c.addFront(new Texture(canvalist.canvas.neck))
		.addRear(new Texture(canvalist.canvas.neck))
		.addLeft(new Texture(canvalist.canvas.neck))
		.addRight(new Texture(canvalist.canvas.neck));
	this.addChild(c);
	this.setPosition(0, 5, 6.6)
	this.setRotation(.37, 0, 0);
}
Model.PonyNeck.prototype = new Model.Object()
Model.PonyNeck.prototype.update = function()
{
	var pos = {
		n: {s: [0, 5, 6.5], b: [0, 2, 7.7], r: [0, 0, 0]},
		f: {s: [0, 3, 4.3], b: [0, 1.85, 5.28], r: [0, 0, 0]},
		l: {s: [0, 6, 8.2], b: [0, 2.5, 9.7], r: [0, 0, 0]}
	};
	
	var k = this.ini.size,
		p = pos[k == 'a' ? 'n' : k][this.ini.pos == 'h' ? 's' : this.ini.pos];
	this.setPosition(p[0], p[1], p[2]);
	
	this.setVisible(!(this.ini.pos == 'r'))
	
	if (this.ini.pos == 'b')
		this.setRotation(.6, 0, 0);
	else
		this.setRotation(.27, 0, 0);

	Model.Object.prototype.update.call(this);
}

// pony body
Model.PonyBody = function(ini, canvalist)
{
	this.ini = ini;
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();

	var c = new Model.Cube(8, 8, 4);
	c.addFront(new Texture(canvalist.canvas.bodyFront))
		.addTop(new Texture(canvalist.canvas.bodyTop))
		.addBottom(new Texture(canvalist.canvas.bodyBottom))
		.addLeft(new Texture(canvalist.canvas.bodyLeft))
		.addRight(new Texture(canvalist.canvas.bodyRight));
	c.setPosition(0, 0, 6);
	this.addChild(c);
	
	c = new Model.Cube(8, 0, 12);
	c.addTop(new Texture(canvalist.canvas.bodyBack));
	c.setPosition(0, 4, -2);
	this.addChild(c);

	c = new Model.Cube(8, 0, 8);
	c.addBottom(new Texture(canvalist.canvas.bodyBelly));
	c.setPosition(0, -4, 0);
	this.addChild(c);

	c = new Model.Cube(8, 0, 4);
	c.addBottom(new Texture(canvalist.canvas.bodySex));
	c.setPosition(0, -4, -6);
	this.addChild(c);

	c = new Model.Cube(8, 8, 0);
	c.addFront(new TextureMul(canvalist.canvas.bodySex, 8, 8));
	c.setPosition(0, 0, -8);
	this.addChild(c);

	c = new Model.Cube(8, 8, 8);
	c.addLeft(new Texture(canvalist.canvas.bodySide))
		.addRight(new Texture(canvalist.canvas.bodySide), true);
	this.addChild(c);

	c = new Model.Cube(8, 8, 4);
	c.addLeft(new Texture(canvalist.canvas.bodyCMark))
		.addRight(new Texture(canvalist.canvas.bodyCMark), true);
	c.setPosition(0, 0, -6);
	this.addChild(c);
}
Model.PonyBody.prototype = new Model.Object()
Model.PonyBody.prototype.update = function()
{
	//size
	switch (this.ini.size)
	{
		case 'n':
		case 'a':
			this.setScale(1, 1, 1)
			break;
		case 'f':
			this.setScale(0.7)
			break;
		case 'l':
			this.setScale(1.2, 1.2, 1.2)
			break;
	}
	
	if (this.ini.pos == 'b')
		this.setRotation(.4, 0, 0)
	else
		this.setRotation(0, 0, 0)

	Model.Object.prototype.update.call(this);
}

// pony legs
Model.PonyLegs = function(ini, canvalist)
{
	this.ini = ini;
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();
	
	//fore
	var f = new Model.Object();
	f.object3D = new THREE.Object3D();

	var c = new Model.Cube(4, 12, 4);
	c.addFront(new Texture(canvalist.canvas.forelegFront))
		.addRear(new Texture(canvalist.canvas.forelegRear))
		.addLeft(new Texture(canvalist.canvas.forelegLeft))
		.addRight(new Texture(canvalist.canvas.forelegRight))
		.addTop(new Texture(canvalist.canvas.forelegTop))
		.addBottom(new Texture(canvalist.canvas.forelegBottom));
	c.setPosition(-2, 0, 0);
	f.addChild(c);
	f.right = c;
	
	var c = new Model.Cube(4, 12, 4);
	c.addFront(new Texture(canvalist.canvas.forelegFront))
		.addRear(new Texture(canvalist.canvas.forelegRear))
		.addLeft(new Texture(canvalist.canvas.forelegLeft))
		.addRight(new Texture(canvalist.canvas.forelegRight))
		.addTop(new Texture(canvalist.canvas.forelegTop))
		.addBottom(new Texture(canvalist.canvas.forelegBottom));
	c.toleft();
	c.setPosition(2, 0, 0);
	f.addChild(c);
	f.left = c;
	
	f.setPosition(0, -10, 5);
	this.addChild(f);
	this.fore = f;
	
	//hind
	var h = new Model.Object();
	h.object3D = new THREE.Object3D();

	var c = new Model.Cube(4, 12, 4);
	c.addFront(new Texture(canvalist.canvas.hindlegFront))
		.addRear(new Texture(canvalist.canvas.hindlegRear))
		.addLeft(new Texture(canvalist.canvas.hindlegLeft))
		.addRight(new Texture(canvalist.canvas.hindlegRight))
		.addTop(new Texture(canvalist.canvas.hindlegTop))
		.addBottom(new Texture(canvalist.canvas.hindlegBottom));
	c.setPosition(-2, 0, 0);
	h.addChild(c);
	h.right = c;
	
	var c = new Model.Cube(4, 12, 4);
	c.addFront(new Texture(canvalist.canvas.hindlegFront))
		.addRear(new Texture(canvalist.canvas.hindlegRear))
		.addLeft(new Texture(canvalist.canvas.hindlegLeft))
		.addRight(new Texture(canvalist.canvas.hindlegRight))
		.addTop(new Texture(canvalist.canvas.hindlegTop))
		.addBottom(new Texture(canvalist.canvas.hindlegBottom));
	c.toleft();
	c.setPosition(2, 0, 0);
	h.addChild(c);
	h.left = c;
	
	h.setPosition(0, -10, -4);
	this.addChild(h);
	this.hind = h;
}
Model.PonyLegs.prototype = new Model.Object()
Model.PonyLegs.prototype.update = function()
{
	//size
	switch (this.ini.size)
	{
		case 'n':
			this.setScale(1, 1, 1)
			this.setPosition(0, 0, 0)
			break;
		case 'f':
			if (this.ini.pos == 's' || this.ini.pos == 'h')
				this.setScale(0.7, 0.5, 0.7)
			if (this.ini.pos == 'b')
				this.setScale(0.7, 0.5, 0.7)
			if (this.ini.pos == 'r')
				this.setScale(0.7, 0.7, 0.5)
			this.setPosition(0, this.ini.pos == 's' || this.ini.pos == 'h' ? -0.755 : 0, 0)
			break;
		case 'a':
			if (this.ini.pos == 's' || this.ini.pos == 'h')
				this.setScale(1, 1.2, 1)
			if (this.ini.pos == 'b')
				this.setScale(1, 1.2, 1)
			if (this.ini.pos == 'r')
				this.setScale(1, 1, 1.2)
			this.setPosition(0, this.ini.pos == 's' || this.ini.pos == 'h' ? 0.84 : 0, 0)
			break;
		case 'l':
			this.setScale(1.2, 1.2, 1.2)
			this.setPosition(0, 0, 0)
			break;
	}
	
	switch (this.ini.pos)
	{
		case 's':
		case 'h':
			this.fore.right.setPosition(-2, 0, 0);
			this.fore.right.setRotation(0, 0, 0);
			this.fore.left.setPosition(2, 0, 0);
			this.fore.left.setRotation(0, 0, 0);
			this.fore.setPosition(0, -10, 5);
			this.fore.setRotation(0, 0, 0);
			
			this.hind.right.setPosition(-2, 0, 0);
			this.hind.right.setRotation(0, 0, 0);
			this.hind.left.setPosition(2, 0, 0);
			this.hind.left.setRotation(0, 0, 0);
			this.hind.setPosition(0, -10, -4);
			break;
		case 'b':
			this.fore.right.setPosition(-6, 0, 0);
			this.fore.right.setRotation(0, 0, 0);
			this.fore.left.setPosition(6, 0, 0);
			this.fore.left.setRotation(0, 0, 0);
			this.fore.setRotation(-.2, 0, 0);
			this.fore.setPosition(0, this.ini.size == 'f' ? -7 : -5, 6);
			
			this.hind.right.setPosition(-6, 0, 0);
			this.hind.right.setRotation(0, 0, 0);
			this.hind.left.setPosition(6, 0, 0);
			this.hind.left.setRotation(0, 0, 0);
			this.hind.setRotation(0, 0, 0);
			this.hind.setPosition(0, this.ini.size == 'f' ? -7 : -5, -5.4);
			break;
		case 'r':
			this.fore.right.setRotation(Math.PI / -2, 0, 0);
			this.fore.right.setPosition(-3.5, 0, 0);
			this.fore.left.setRotation(Math.PI / -2, 0, 0);
			this.fore.left.setPosition(3.5, 0, 0);
			this.fore.setPosition(0, -2.05, 9);
			this.fore.setRotation(0, 0, 0);
			
			this.hind.right.setRotation(Math.PI / 2, 0, 0);
			this.hind.right.setPosition(-3.5, 0, 0);
			this.hind.left.setRotation(Math.PI / 2, 0, 0);
			this.hind.left.setPosition(3.5, 0, 0);
			this.hind.setPosition(0, -2.05, -6);
			this.hind.setRotation(0, 0, 0);
			break;
	}
	
	if (this.ini.pos == 'h' && this.ini.type != 'u' && this.ini.type != 'a')
	{
		this.fore.right.setRotation(-.3, 0, -.025)
		this.fore.right.setPosition(-2.1, .5, 2)
	}
	
	Model.Object.prototype.update.call(this);
}

// pony tail
Model.PonyTail = function(ini, canvalist)
{
	this.ini = ini;
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();
	
	this.tassel = [];
	
	var c = new Model.Cube(4, 16, 4);
	c.basic = false;
	c.addFront(new TextureMul(canvalist.canvas.tailFront, 4, 16))
		.addRear(new TextureMul(canvalist.canvas.tailFront, 4, 16), true)
		.addRight(new TextureMul(canvalist.canvas.tailLeft, 4, 16), true)
		.addLeft(new TextureMul(canvalist.canvas.tailLeft, 4, 16))
		.addTop(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4))
		.addBottom(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4), true);
	c.setPosition(0, -2, -12);
	this.addChild(c);
	this.tassel[4] = c;

	var c = new Model.Cube(4, 12, 4);
	c.basic = false;
	c.addFront(new TextureMul(canvalist.canvas.tailFront, 4, 12))
		.addRear(new TextureMul(canvalist.canvas.tailFront, 4, 12), true)
		.addRight(new TextureMul(canvalist.canvas.tailLeft, 4, 12), true)
		.addLeft(new TextureMul(canvalist.canvas.tailLeft, 4, 12))
		.addTop(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4))
		.addBottom(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4), true);
	c.setPosition(0, 0, -12);
	this.addChild(c);
	this.tassel[3] = c;
	
	var c = new Model.Cube(4, 8, 4);
	c.basic = false;
	c.addFront(new Texture(canvalist.canvas.tailFront))
		.addRear(new Texture(canvalist.canvas.tailFront), true)
		.addRight(new Texture(canvalist.canvas.tailLeft), true)
		.addLeft(new Texture(canvalist.canvas.tailLeft))
		.addTop(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4))
		.addBottom(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4), true);
	c.setPosition(0, 2, -12);
	this.addChild(c);
	this.tassel[2] = c;

	var c = new Model.Cube(4, 4, 4);
	c.basic = false;
	c.addFront(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4))
		.addRear(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4), true)
		.addRight(new TextureSub(canvalist.canvas.tailLeft, 0, 0, 4, 4), true)
		.addLeft(new TextureSub(canvalist.canvas.tailLeft, 0, 0, 4, 4))
		.addTop(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4))
		.addBottom(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 4), true);
	c.setPosition(0, 4, -12);
	this.addChild(c);
	this.tassel[1] = c;
	
	var s = new Model.Object();
	s.object3D = new THREE.Object3D();
	
	c = new Model.Cube(2, 2, 6);
	c.basic = false;
	c.addTop(new TextureSub(canvalist.canvas.tailFront, 0, 0, 2, 6))
		.addBottom(new TextureSub(canvalist.canvas.tailFront, 0, 0, 2, 6))
		.addRear(new TextureSub(canvalist.canvas.tailFront, 0, 0, 2, 2), true);
	s.addChild(c);

	c = new Model.Cube(2, 2, 4);
	c.basic = false;
	c.addLeft(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 2))
		.addRight(new TextureSub(canvalist.canvas.tailFront, 0, 0, 4, 2), true)
	c.setPosition(0, 0, 1)
	s.addChild(c);

	c = new Model.Cube(2, 2, 2);
	c.basic = false;
	c.addLeft(new TextureSub(canvalist.canvas.tailLeft, 0, 0, 2, 2))
		.addRight(new TextureSub(canvalist.canvas.tailLeft, 0, 0, 2, 2), true);
	c.setPosition(0, 0, -2)
	s.addChild(c);
	
	s.setPosition(0, 3.4, -9.2);
	s.setRotation(.54, 0, 0);
	
	this.addChild(s);
	this.stic = s;
}
Model.PonyTail.prototype = new Model.Object()
Model.PonyTail.prototype.update = function()
{
	//tail
	for (var i = 1; i < 5; i ++)
		this.tassel[i].setVisible(i == this.ini.tail);
		
	//size
	switch (this.ini.size)
	{
		case 'n':
		case 'a':
			this.setScale(1)
			break;
		case 'f':
			this.setScale(0.7)
			break;
		case 'l':
			this.setScale(1.2)
			break;
	}

	if (this.ini.pos == 'b')
	{
		// bend down
		this.setPosition(0, 4.2, 2.5);
		this.stic.setRotation(.88, 0, 0);
		this.stic.setPosition(0, 2.7, -9.5);
	}
	else
	{
		this.setPosition(0, 0, 0)
		this.stic.setRotation(.54, 0, 0);
		this.stic.setPosition(0, 3.4, -9.2);
	}
	
	Model.Object.prototype.update.call(this);
}

// pony wings
Model.PonyWings = function(ini, canvalist)
{
	this.ini = ini;
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();
	
	function craeteNormalWing(toleft)
	{
		var rightWing = new Model.Object();
		rightWing.object3D = new THREE.Object3D();
		function createPlum(length)
		{
			var c = new Model.Cube(2, length, 2);
			c.addFront(new TextureSub(canvalist.canvas.wingFront, 0, 0, 2, length))
				.addRear(new TextureSub(canvalist.canvas.wingRear, 0, 0, 2, length))
				//.addLeft(new TextureSub(canvalist.canvas.wingLeft, 0, 0, 2, length))
				.addRight(new TextureSub(canvalist.canvas.wingRight, 0, 0, 2, length))
				.addTop(new Texture(canvalist.canvas.wingTop))
				.addBottom(new Texture(canvalist.canvas.wingBottom));
			if (toleft)
				c.toleft();
			return c;
		}
		var c = createPlum(8);
		rightWing.addChild(c);
		var c = createPlum(6);
		c.setPosition(0, 1, -2);
		rightWing.addChild(c);
		var c = createPlum(6);
		c.setPosition(0, 1, 2);
		rightWing.addChild(c);
		rightWing.setRotation(Math.PI / 2, 0, 0);
		return rightWing;
	}
	var rightWing = craeteNormalWing();
	rightWing.setPosition(-5, 0, 0);

	var leftWing = craeteNormalWing(true);
	leftWing.setPosition(5, 0, 0);
	
	var c = new Model.Object;
	c.object3D = new THREE.Object3D();
	c.addChild(rightWing);
	c.addChild(leftWing);
	this.addChild(c);
	this.closed = c;
	
	function createOpenWing()
	{
		var w = new Model.Object();
		w.object3D = new THREE.Object3D();
		
		function createW1(len)
		{
			var c = new Model.Cube(1, len, 2);
			c.addFront(new TextureSub(canvalist.canvas.wingFront, 1, 3, 1, len))
				.addRight(new TextureSub(canvalist.canvas.wingRight, 0, 3, 2, len), true)
				.addLeft(new TextureSub(canvalist.canvas.wingLeft, 0, 3, 2, len))
				.addRear(new TextureSub(canvalist.canvas.wingRear, 1, 3, 1, len))
				.addBottom(new TextureSub(canvalist.canvas.wingFront, 1, 1, 1, 2))
				.addTop(new TextureSub(canvalist.canvas.wingFront, 1, 1, 1, 2), true)
			return c;
		}
		function createW2()
		{
			var c = new Model.Cube(1, 8, 2);
			c.addFront(new TextureSub(canvalist.canvas.wingLeft, 1, 3, 1, 8))
				.addRight(new TextureSub(canvalist.canvas.wingRight, 0, 3, 2, 8), true)
				.addLeft(new TextureSub(canvalist.canvas.wingFront, 0, 3, 2, 8))
				.addRear(new TextureSub(canvalist.canvas.wingRear, 1, 3, 1, 8))
				.addBottom(new TextureSub(canvalist.canvas.wingFront, 1, 1, 1, 2))
			return c;
		}

		var c = createW1(6);
		c.setScale(2, 1, 1.2)
		c.setPosition(0, 0, 0)
		w.addChild(c);

		var c = createW2();
		c.setScale(1.5, 1, 1)
		c.setPosition(0, -1, 0.2)
		w.addChild(c);

		var c = createW1(6);
		c.setScale(1.5, 1, 1)
		c.setPosition(0, -8, 0.2)
		w.addChild(c);
		
		var o = new Model.Object();
		o.object3D = new THREE.Object3D();

		var c = createW1(3);
		c.setScale(1.9, 1, 1)
		c.setPosition(0, 1.5, .4)
		o.addChild(c);
		
		var c = createW2();
		c.setScale(.8)
		o.addChild(c);
		
		w.addChild(o)
		o.setRotation(Math.PI / 2 / 1.6, 0, 0)
		o.setPosition(0, .9, -2.5)
		
		var c = createW2();
		c.setScale(.9)
		c.setRotation(Math.PI / 2 / 2.2, 0, 0)
		c.setPosition(0, -2.1, -2.7)
		w.addChild(c);

		var c = createW2();
		c.setRotation(Math.PI / 2 / 2 / 1.8, 0, 0)
		c.setPosition(0, -6, -2)
		w.addChild(c);
		return w;
	}
	
	var c = new Model.Object;
	c.object3D = new THREE.Object3D();
	
	var w = createOpenWing()
	w.setPosition(-5, 0, 0);
	w.setRotation(0, 0, 3.28)
	c.addChild(w);

	var w = createOpenWing()
	w.setPosition(5, 0, 0);
	w.setRotation(0, 0, 3)
	c.addChild(w);
	
	c.setPosition(0, 3.5, 1)
	c.setRotation(-.45, 0, 0)
	
	this.addChild(c);
	this.opened = c;
}
Model.PonyWings.prototype = new Model.Object()
Model.PonyWings.prototype.update = function()
{
	if (this.ini.type == 'a' || this.ini.type == 'p')
	{
		this.setVisible(true);
		var bClosed = this.ini.pos != 'b';
		this.closed.setVisible(bClosed);
		this.opened.setVisible(!bClosed);
		
		//size
		switch (this.ini.size)
		{
			case 'n':
			case 'a':
				this.setScale(1)
				break;
			case 'f':
				this.setScale(0.7)
				break;
			case 'l':
				this.setScale(1.2)
				break;
		}
	}
	else 
	{
		this.setVisible(false);
	}
	
	Model.Object.prototype.update.call(this);
}

//
Texture = function(canvas)
{
	this.canvas = canvas;
}

Texture.prototype.update = function() { }

TextureSub = function(canvas, x, y, width, height)
{
	this.source = canvas;
	this.canvas = document.createElement('canvas');
	this.canvas.width = width;
	this.canvas.height = height;
	this.x = x;
	this.y = y;
	this.w = width;
	this.h = height;
	this.sContext = this.source.getContext("2d");
	this.cContext = this.canvas.getContext("2d");
}

TextureSub.prototype.update = function()
{
	this.cContext.putImageData(this.sContext.getImageData(this.x, this.y, this.w, this.h), 0, 0);
}

TextureMul = function(canvas, width, height)
{
	this.source = canvas;
	this.canvas = document.createElement('canvas');
	this.canvas.width = width;
	this.canvas.height = height;
	this.w = width;
	this.h = height;
	this.sContext = this.source.getContext("2d");
	this.cContext = this.canvas.getContext("2d");
}

TextureMul.prototype.update = function()
{
	var x = 0,
		y = 0;
	while (y < this.h)
	{
		x = 0;
		while (x < this.w)
		{
			this.cContext.putImageData(this.sContext.getImageData(0, 0, this.source.width, this.source.height), x, y);
			x += this.source.width;
		}
		y += this.source.height;
	}
}

// model Item
Model.Item = function(colorContext, itemsContext, ini)
{
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();
	
	this.ini = ini;
	
	this.colorContext = colorContext;
	this.context = itemsContext;

	this.cnt = new Model.Object();
	this.cnt.object3D = new THREE.Object3D();

	this.base = new Model.Object();
	this.base.object3D = new THREE.Object3D();
	this.cnt.addChild(this.base);

	this.glow = new Model.Object();
	this.glow.object3D = new THREE.Object3D();
	this.cnt.addChild(this.glow);
	this.addChild(this.cnt);
}
Model.Item.prototype = new Model.Object()
Model.Item.prototype.update = function()
{
	var c = this.colorContext.getImageData(0, 0, 1, 1).data;
	var color = c[3] ? ((1 << 24) + (c[0] << 16) + (c[1] << 8) + c[2]) : 0x4444AB;
	for (var i = 0; i < this.glow.children.length; i ++)
		this.glow.children[i].update(color);
	
	this.setVisible(this.ini.pos == 'h');
	
	this.glow.setVisible(this.ini.pos == 'h' && (this.ini.type == 'u' || this.ini.type == 'a'))
	
	if (this.ini.pos == 'h')
	{
		var off = 0;
		this.setScale(this.ini.size == 'f' ? 1.1 : 1);
		if (this.r < 5)
		{
			if (this.ini.type == 'u' || this.ini.type == 'a')
			{
				this.setRotation(1.4, -.2, 0);
			}
			else
			{
				this.setRotation(1.2, -.2, 0);
				off = 1.5;
			}
		}
		else
			this.setRotation(.1, .2, 0);
		if (this.ini.type == 'u' || this.ini.type == 'a')
		{
			switch (this.ini.size)
			{
				case 'f':
					this.setPosition(0, 0, 18)
					break;
				case 'n':
					this.setPosition(0, -4, 17)
					break
				case 'l':
					this.setPosition(0, -5, 19)
					break;
				case 'a':
					this.setPosition(0, -5, 17)
			}
		}
		else
		{
			switch (this.ini.size)
			{
				case 'f':
					this.setPosition(-1, -6 + off, 11 + off)
					break;
				case 'n':
					this.setPosition(-2, -13.5 + off, 11.5 + off)
					break
				case 'l':
					this.setPosition(-2, -15.5 + off, 13.5 + off)
					break;
				case 'a':
					this.setPosition(-2, -15.5 + off, 11.5 + off)
					break;
			}
		}
	}
}
Model.Item.prototype.create = function()
{
	// remove old model
	for (var i = this.base.children.length - 1; i >= 0 ; i --)
		this.base.removeChild(this.base.children[i]);
	for (var i = this.glow.children.length - 1; i >= 0 ; i --)
		this.glow.removeChild(this.glow.children[i]);
	//	
	var canvas = this.context.canvas,
		r = Math.floor((Math.random() * 6) + 1) - 1,
		id = this.context.getImageData(0, r * 16, 16, 16).data,
		y = 0, x = 0,
		map = [];
	this.cnt.setRotation(1.2, r < 5 ? Math.PI / 2 : Math.PI - 0.4, r < 5 ? -.4 : 0)
	this.cnt.setScale(r < 5 ? 0.8 : .5);
	this.r = r;
	for (var i = 0; i < id.length; i += 4)
	{
		if (!map[y])
			map[y] = [];
		map[y][x] = id[i + 3] ? ((1 << 24) + (id[i] << 16) + (id[i + 1] << 8) + id[i + 2]) : null;
		x ++;
		if (x == canvas.width)
		{
			y ++;
			x = 0;
		}
	}
	var pX, spX = 16 / -2, pY = 16 / 2;
	for (var y = 0; y < 16; y ++)
	{
		pX = spX;
		for (var x = 0; x < 16; x ++)
		{
			if (!map[y][x])
				continue;
			var c = new Model.iCube(map[y][x]);
			var g = new Model.iCube(null, Model.isCanvasRender ? .18 : .4);
			var s = false;
			c.addFront().addRear();
			g.addFront().addRear();
			if (!map[y][x - 1])
			{
				c.addRight();
				g.addRight();
				s = true;
			}
			if (!map[y][x + 1])
			{
				c.addLeft();
				g.addLeft();
				s = true;
			}
			if (!y || !map[y - 1][x])
			{
				c.addTop();
				g.addTop();
				s = true;
			}
			if (!map[y + 1] || !map[y + 1][x])
			{
				c.addBottom();
				g.addBottom();
				s = true;
			}
			c.setPosition(pX + x, pY, 0);
			g.setPosition(pX + x, pY, 0);
			if (!s)
				g.setScale(1, 1, 1.5);
			else
				g.setScale(1.5);
			this.base.addChild(c);
			this.glow.addChild(g);
		}
		pY -= 1;
	}
}

// Model Glow
Model.Glow = function(context)
{
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();
	this.context = context;
	function makeGlow(b, c)
	{
		var material = new THREE.MeshBasicMaterial({vertexColors: THREE.FaceColors, opacity: Model.isCanvasRender ? .18 : .4, transparent: true});
		// radiusAtTop, radiusAtBottom, height, segmentsAroundRadius, segmentsAlongHeight,
		var geometry = new THREE.CylinderGeometry(1, b, c, 4, 1);
		return new THREE.Mesh(geometry, material);
	}
	var mesh = makeGlow(1.5, 5);
	mesh.position.y = 0;
	this.object3D.add( mesh );
	mesh = makeGlow(2, 4.5);
	mesh.position.y = 0.75;
	this.object3D.add( mesh );
	this.setRotation(0, Math.PI / 2 / 2, 0)
}

Model.Glow.prototype = new Model.Object();
Model.Glow.prototype.update = function()
{
	var c = this.context.getImageData(0, 0, 1, 1).data;
	var glowColor = c[3] ? ((1 << 24) + (c[0] << 16) + (c[1] << 8) + c[2]) : 0x4444AB;
	for (var i = 0; i < this.object3D.children.length; i ++)
	{
		var child = this.object3D.children[i];
		for (var g = 0; g < child.geometry.faces.length; g ++)
			child.geometry.faces[g].color.setHex(glowColor);
		child.geometry.colorsNeedUpdate = true;
	}
}

// Model iCube
Model.iCube = function(color, opacity)
{
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();
	
	this.data = {};
	this.color = color;
	this.opacity = opacity || 1;
}
Model.iCube.prototype = new Model.Object();

Model.iCube.prototype.addFront = function()
{
	var mesh = this.createPlane();
	mesh.position.z = .5;
	return this;
}

Model.iCube.prototype.addRear = function()
{
	var mesh = this.createPlane()
	mesh.position.z = -.5;
	mesh.rotation.y = Math.PI * -1;
	return this;
}

Model.iCube.prototype.addRight = function()
{
	var mesh = this.createPlane()
	mesh.position.x = -.5;
	mesh.rotation.y = Math.PI / -2;
	return this;
}

Model.iCube.prototype.addLeft = function()
{
	var mesh = this.createPlane()
	mesh.position.x = .5;
	mesh.rotation.y = Math.PI / 2;
	return this;
}

Model.iCube.prototype.addTop = function()
{
	var mesh = this.createPlane()
	mesh.position.y = .5;
	mesh.rotation.x = Math.PI / -2;
	return this;
}

Model.iCube.prototype.addBottom = function()
{
	var mesh = this.createPlane();
	mesh.position.y = -.5;
	mesh.rotation.x = Math.PI / 2;
	return this;
}

Model.iCube.prototype.update = function(color)
{
	color = color || this.color;
	for (var i = 0; i < this.object3D.children.length; i ++)
	{
		var child = this.object3D.children[i];
		for (var g = 0; g < child.geometry.faces.length; g ++)
			child.geometry.faces[g].color.setHex(color);
		child.geometry.colorsNeedUpdate = true;
	}
}

Model.iCube.prototype.createPlane = function()
{
	var material = new THREE.MeshPhongMaterial({vertexColors: THREE.FaceColors, side: THREE.DoubleSide, opacity: this.opacity, transparent: true});
	var geometry = new Model.Side(1, 1);
	if (this.color)
	{
		geometry.faces[0].color.setHex(this.color);
		geometry.colorsNeedUpdate = true;
	}
	var mesh = new THREE.Mesh(geometry, material);
	this.object3D.add(mesh);
	return mesh;
}

// Model cube
Model.Cube = function(width, height, depth)
{
	Model.Object.call(this);
	this.object3D = new THREE.Object3D();
	
	this.width = width;
	this.height = height;
	this.depth = depth;
	
	this.basic = !Model.isCanvasRender;
	this.transparent = true;
	this.data = {};
}

Model.Cube.prototype = new Model.Object();

Model.Cube.prototype.toleft = function()
{
	var data;
	if (data = this.data['top'])
		data.mesh.rotation.y = Math.PI;
	if (data = this.data['bottom'])
		data.mesh.rotation.y = Math.PI * -1;
	if (data = this.data['left'])
		data.mesh.position.x = this.width / 2 * -1;
	if (data = this.data['right'])
		data.mesh.position.x = this.width / 2;
	if (data = this.data['front'])
		data.mesh.rotation.y = Math.PI;
	if (data = this.data['rear'])
		data.mesh.rotation.y = 0;
}

Model.Cube.prototype.update = function()
{
	var side = ['top', 'bottom', 'left', 'right', 'rear', 'front'];
	for (var s = 0; s < side.length; s ++)
	{
		var data;
		if (data = this.data[side[s]])
		{
			data.txtr.update();
			if (this.basic)
				data.texture.needsUpdate = true;
			else
			{
				var canvas = data.txtr.canvas;
				var imageData = canvas.getContext("2d").getImageData(0, 0, canvas.width, canvas.height),
					pix = imageData.data,
					childs = data.mesh.children,
					f = 0;
				if (data.mesh.visible)
				{
					for (var i = 0, n = pix.length; i < n; i += 4)
					{
						var child = childs[f ++],
							g = child.geometry;
						g.faces[0].color.setHex((1 << 24) + (pix[i] << 16) + (pix[i+1] << 8) + pix[i+2]);
						child.visible = pix[i + 3] > 0;
						g.colorsNeedUpdate = true;
					}
				}
			}
		}
	}
	return this;
}

Model.Cube.prototype.addFront = function(texture)
{
	var mesh = this.createPlane('front', texture);
	mesh.position.z = this.depth / 2 * 1;
	return this;
}

Model.Cube.prototype.addRear = function(texture, flip)
{
	var mesh = this.createPlane('rear', texture)
	mesh.position.z = this.depth / 2 * -1;
	mesh.rotation.y = !flip ? Math.PI * -1 : Math.PI * -2;
	return this;
}

Model.Cube.prototype.addRight = function(texture, flip)
{
	var mesh = this.createPlane('right', texture)
	mesh.position.x = this.width / 2 * -1;
	mesh.rotation.y = !flip ? Math.PI / -2 : Math.PI / 2;
	return this;
}

Model.Cube.prototype.addLeft = function(texture)
{
	var mesh = this.createPlane('left', texture)
	mesh.position.x = this.width / 2 * 1;
	mesh.rotation.y = Math.PI / 2;
	return this;
}

Model.Cube.prototype.addTop = function(texture, flipX)
{
	var mesh = this.createPlane('top', texture)
	mesh.position.y = this.height / 2;
	mesh.rotation.x = !flipX ? Math.PI / -2 : Math.PI / 2;
	return this;
}

Model.Cube.prototype.addBottom = function(texture, flipX)
{
	var mesh = this.createPlane('bottom', texture)
	mesh.position.y = this.height / 2 * -1;
	mesh.rotation.x = !flipX ? Math.PI / 2 : Math.PI / -2;
	return this;
}

Model.Cube.prototype.createPlane = function(side, txtr)
{
	var data = {
			width: txtr.canvas.width,
			height: txtr.canvas.height,
			txtr: txtr
		},
		geometry, material;
	if (this.basic)
	{
		var texture = new THREE.Texture(txtr.canvas);
		texture.magFilter = THREE.NearestFilter;
		texture.minFilter = THREE.LinearMipMapLinearFilter;
		material = new THREE.MeshPhongMaterial({map: texture, transparent: this.transparent, vertexColors: THREE.FaceColors, side: THREE.DoubleSide});
		data.texture = texture;
		geometry = new THREE.PlaneGeometry(data.width, data.height);
		data.mesh = new THREE.Mesh(geometry, material);
	}
	else
	{
		data.mesh = new THREE.Object3D();
		var pX = data.width / -2;
		var pY = data.height / -2;
		for (var y = 0; y < data.height; y ++)
		{
			for (var x = 0; x < data.width; x ++)
			{
				material = new THREE.MeshPhongMaterial({vertexColors: THREE.FaceColors, side: THREE.DoubleSide});
				geometry = new Model.Side(1, 1);
				var mesh = new THREE.Mesh(geometry, material);
				mesh.position.x = pX + x + 0.5;
				mesh.position.y = pY + data.height - y - 0.5;
				data.mesh.add(mesh)
			}
		}
	}
	data.mesh.name = side;
	this.object3D.add(data.mesh);
	this.data[side] = data;
	return data.mesh;
}

// model side geometry
Model.Side = function (width, height, widthParts, heightParts)
{
	THREE.Geometry.call(this);
	widthParts = widthParts || 1;
	heightParts = heightParts || 1;
	
	var x2 = width / -2;
	var y2 = height / -2;
	
	var w, h, wStep = width / widthParts, hStep = height / heightParts, i = 0;
	for (h = heightParts - 1; h >= 0; h --)
	{
		for (w = 0; w < widthParts; w ++)
		{
			var fX1 = w * wStep + x2;
			var fY1 = h * hStep + y2;

			var fX2 = fX1 + wStep;
			var fY2 = fY1 + hStep;

			this.vertices.push(new THREE.Vector3(fX1, fY1, 0));
			this.vertices.push(new THREE.Vector3(fX2, fY1, 0));
			this.vertices.push(new THREE.Vector3(fX2, fY2, 0));
			this.vertices.push(new THREE.Vector3(fX1, fY2, 0));
		}
	}
	
	for (w = 0; w < widthParts * heightParts; w ++)
		this.faces.push(new THREE.Face4( w * 4, w * 4 + 1, w * 4 + 2, w * 4 + 3));

	this.computeCentroids();
	this.computeFaceNormals();
}

Model.Side.prototype = new THREE.Geometry();