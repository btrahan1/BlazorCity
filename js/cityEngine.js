window.CityEngine = {
    scene: null,
    camera: null,
    renderer: null,
    gridSize: 20,
    tileSize: 10,
    raycaster: new THREE.Raycaster(),
    mouse: new THREE.Vector2(),
    cursor: null,
    selectedBuilding: null,
    dotNetRef: null,

    // Debug flags
    debug: true,

    log: function (msg) {
        if (this.debug) console.log("[CityEngine] " + msg);
    },

    setSelectedBuilding: function (filename) {
        this.selectedBuilding = filename;
        this.log("Selected building: " + filename);
        if (this.cursor) {
            this.cursor.material.color.setHex(filename ? 0x00ff00 : 0xffff00);
            this.cursor.material.opacity = filename ? 0.8 : 0.4;
        }
    },

    init: async function (containerId, dotNetRef, savedGridSize) {
        this.log("Initializing v9 with Interop...");
        this.dotNetRef = dotNetRef;
        if (savedGridSize) this.gridSize = savedGridSize;

        const container = document.getElementById(containerId);
        if (!container) return;

        // Cleanup
        while (container.firstChild) container.removeChild(container.firstChild);

        // Scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x87CEEB);

        // Camera
        const aspect = container.clientWidth / container.clientHeight;
        const d = 150;
        this.camera = new THREE.OrthographicCamera(-d * aspect, d * aspect, d, -d, 1, 1000);
        this.camera.position.set(200, 200, 200);
        this.camera.lookAt(this.scene.position);
        this.camera.zoom = 1.0;
        this.camera.updateProjectionMatrix();

        // Renderer
        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setSize(container.clientWidth, container.clientHeight);
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        container.appendChild(this.renderer.domElement);

        // Lighting
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
        this.scene.add(ambientLight);
        const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
        dirLight.position.set(100, 200, 50);
        dirLight.castShadow = true;

        // Shadow settings
        const shadowSize = 250;
        dirLight.shadow.camera.left = -shadowSize;
        dirLight.shadow.camera.right = shadowSize;
        dirLight.shadow.camera.top = shadowSize;
        dirLight.shadow.camera.bottom = -shadowSize;
        dirLight.shadow.mapSize.width = 2048;
        dirLight.shadow.mapSize.height = 2048;
        this.scene.add(dirLight);

        // Ground
        this.createGrid();

        // Cursor
        const cursorGeo = new THREE.BoxGeometry(this.tileSize, 1, this.tileSize);
        const cursorMat = new THREE.MeshBasicMaterial({ color: 0xffff00, opacity: 0.4, transparent: true });
        this.cursor = new THREE.Mesh(cursorGeo, cursorMat);
        this.cursor.visible = false;
        this.scene.add(this.cursor);

        // --- INPUT HANDLING ---

        // 1. Hover (Raycasting)
        this.renderer.domElement.addEventListener('pointermove', (event) => {
            event.preventDefault();
            const rect = this.renderer.domElement.getBoundingClientRect();
            this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
            this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
        });

        // 2. Click (Building)
        let downTime = 0;
        this.renderer.domElement.addEventListener('pointerdown', (event) => {
            downTime = Date.now();
        });

        this.renderer.domElement.addEventListener('pointerup', async (event) => {
            const clickDuration = Date.now() - downTime;
            if (clickDuration > 250) return;
            if (event.button !== 0) return;
            if (!this.selectedBuilding || !this.cursor.visible) return;

            // Calculate Grid FIRST
            const worldX = this.cursor.position.x;
            const worldZ = this.cursor.position.z;
            const gx = Math.round(((worldX - this.tileSize / 2) / this.tileSize) + this.gridSize / 2);
            const gz = Math.round(((worldZ - this.tileSize / 2) / this.tileSize) + this.gridSize / 2);

            // Purchase Check
            let authorized = true;
            if (this.dotNetRef) {
                this.log(`Attempting purchase for ${gx},${gz}...`);
                try {
                    // Pass coords to C#
                    authorized = await this.dotNetRef.invokeMethodAsync('TryPurchase', this.selectedBuilding, gx, gz);
                    this.log("Purchase Result: " + authorized);
                } catch (err) {
                    console.error("Interop Error:", err);
                }
            } else {
                this.log("WARNING: No dotNetRef found! Bypassing purchase check.");
            }

            if (!authorized) {
                this.log("Purchase denied: Insufficient funds.");
                // TODO: Visual feedback for failure
                return;
            }

            // Place
            this.log(`PLACING BUILDING: ${this.selectedBuilding} at Grid[${gx},${gz}]`);
            await this.placeBuilding(this.selectedBuilding, gx, gz);
        });

        // Load Demo
        await this.demoCity();

        // Controls
        const controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
        controls.enableDamping = true;
        controls.dampingFactor = 0.05;
        controls.enableZoom = true;

        // Loop
        const animate = () => {
            requestAnimationFrame(animate);
            controls.update();

            // Raycasting Logic
            this.raycaster.setFromCamera(this.mouse, this.camera);
            const intersects = this.raycaster.intersectObjects(this.scene.children);

            let found = false;
            for (let i = 0; i < intersects.length; i++) {
                // We want to hit the ground plane or existing objects near y=0
                // For simplicity, let's just use the ground plane intersection point
                const p = intersects[i].point;
                if (Math.abs(p.y) < 10) { // Tolerance
                    const snapX = Math.floor(p.x / this.tileSize) * this.tileSize + (this.tileSize / 2);
                    const snapZ = Math.floor(p.z / this.tileSize) * this.tileSize + (this.tileSize / 2);

                    if (this.cursor) {
                        this.cursor.position.set(snapX, 0.5, snapZ);
                        this.cursor.visible = true;
                    }
                    found = true;
                    break;
                }
            }
            if (!found && this.cursor) this.cursor.visible = false;

            this.renderer.render(this.scene, this.camera);
        };
        animate();

        window.addEventListener('resize', () => {
            const aspect = container.clientWidth / container.clientHeight;
            this.camera.left = -d * aspect;
            this.camera.right = d * aspect;
            this.camera.top = d;
            this.camera.bottom = -d;
            this.camera.updateProjectionMatrix();
            this.renderer.setSize(container.clientWidth, container.clientHeight);
        });
    },

    createGrid: function () {
        const planeGeo = new THREE.PlaneGeometry(this.gridSize * this.tileSize, this.gridSize * this.tileSize);
        const planeMat = new THREE.MeshStandardMaterial({ color: 0x4caf50 });
        const plane = new THREE.Mesh(planeGeo, planeMat);
        plane.rotation.x = -Math.PI / 2;
        plane.receiveShadow = true;
        plane.name = "Ground"; // Helpful for raycasting
        this.scene.add(plane);

        const gridHelper = new THREE.GridHelper(this.gridSize * this.tileSize, this.gridSize, 0x000000, 0x000000);
        gridHelper.material.opacity = 0.2;
        gridHelper.material.transparent = true;
        gridHelper.position.y = 0.1;
        this.scene.add(gridHelper);
    },

    loadProp: async function (url) {
        try {
            const response = await fetch(url);
            if (!response.ok) return null;

            const data = await response.json();
            const group = new THREE.Group();

            if (data.Parts) {
                data.Parts.forEach(part => {
                    let geometry;
                    if (part.Shape === 'Sphere') geometry = new THREE.SphereGeometry(1, 16, 16);
                    else if (part.Shape === 'Torus') geometry = new THREE.TorusGeometry(0.5, 0.2, 8, 16);
                    else if (part.Shape === 'Cylinder') geometry = new THREE.CylinderGeometry(0.5, 0.5, 1, 16);
                    else if (part.Shape === 'Cone') geometry = new THREE.ConeGeometry(0.5, 1, 16);
                    else geometry = new THREE.BoxGeometry(1, 1, 1);

                    const material = new THREE.MeshStandardMaterial({
                        color: part.ColorHex || 0x888888,
                        roughness: 0.4,
                        metalness: part.Material === 'Metal' ? 0.8 : 0.1,
                        emissive: part.Material === 'Glow' ? (part.ColorHex || 0xffffff) : 0x000000
                    });

                    const mesh = new THREE.Mesh(geometry, material);

                    if (part.Position) mesh.position.set(part.Position[0], part.Position[1], part.Position[2]);
                    if (part.Rotation) {
                        mesh.rotation.x = part.Rotation[0] * (Math.PI / 180);
                        mesh.rotation.y = part.Rotation[1] * (Math.PI / 180);
                        mesh.rotation.z = part.Rotation[2] * (Math.PI / 180);
                    }
                    if (part.Scale) mesh.scale.set(part.Scale[0], part.Scale[1], part.Scale[2]);

                    mesh.castShadow = true;
                    mesh.receiveShadow = true;
                    group.add(mesh);
                });
            }
            return group;
        } catch (e) {
            console.error("Error loading prop:", url, e);
            return null;
        }
    },

    placeBuilding: async function (filename, x, z, rotation = 0) {
        this.log(`Spawning ${filename} at ${x},${z}`);
        const prop = await this.loadProp(`props/${filename}`);
        if (prop) {
            const worldX = (x - this.gridSize / 2) * this.tileSize + (this.tileSize / 2);
            const worldZ = (z - this.gridSize / 2) * this.tileSize + (this.tileSize / 2);

            // Lift slightly to avoid z-fighting with ground
            prop.position.set(worldX, 0.05, worldZ);
            prop.rotation.y = rotation * (Math.PI / 180);

            // Animation: Pop in
            prop.scale.set(0.1, 0.1, 0.1);
            this.scene.add(prop);

            // Simple pop-in effect
            let s = 0.1;
            const pop = () => {
                s += 0.1;
                if (s < 1.0) {
                    prop.scale.set(s, s, s);
                    requestAnimationFrame(pop);
                } else {
                    prop.scale.set(1, 1, 1);
                }
            };
            pop();
        }
    },

    demoCity: async function () {
        console.log("Building Demo City...");
        await this.placeBuilding('SuburbanHomeHiFi_20260128_181032.json', 2, 2);
        await this.placeBuilding('SuburbanHomeHiFi_20260128_181032.json', 3, 2);
        await this.placeBuilding('ModernApartmentComplex_20260128_181155.json', 2, 4, 90);
        await this.placeBuilding('MetropolitanPoliceStation_20260128_180800.json', 8, 8);
        console.log("Demo City Built.");
    }
};
