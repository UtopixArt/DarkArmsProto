# BepuPhysics v2 - Guide Complet

## 🎮 Système de Physique Intégré

Votre projet dispose maintenant d'un système de physique professionnel basé sur **BepuPhysics v2**, une librairie ultra-performante pour .NET.

---

## 📦 Composants Disponibles

### 1. **PhysicsSystem** - Système Principal
Gère la simulation physique globale.

**Fichier:** [Systems/PhysicsSystem.cs](Systems/PhysicsSystem.cs)

**Déjà intégré dans Game.cs :**
- Initialisé automatiquement
- Update à chaque frame
- Dispose au cleanup

### 2. **PhysicsShapeComponent** - Formes de Collision
Définit la forme physique d'un objet.

**Fichier:** [Components/PhysicsShapeComponent.cs](Components/PhysicsShapeComponent.cs)

**Formes disponibles :**
```csharp
SetSphere(radius)              // Sphère - pour projectiles
SetBox(size)                    // Boîte - pour objets cubiques
SetCapsule(radius, height)      // Capsule - RECOMMANDÉ pour personnages
SetCylinder(radius, height)     // Cylindre - pour objets cylindriques
```

### 3. **RigidbodyComponent** - Corps Dynamique
Wrapper pour les corps physiques de BepuPhysics.

**Fichier:** [Components/RigidbodyComponent.cs](Components/RigidbodyComponent.cs)

**Modes :**
- **Dynamic** : Physique complète (gravité, forces, collisions)
- **Kinematic** : Contrôlé par code, pas affecté par forces

**Méthodes utiles :**
```csharp
SetVelocity(velocity)     // Définir vélocité directement
AddForce(force)           // Ajouter force (accélération)
AddImpulse(impulse)       // Ajouter impulsion (instantané)
Teleport(position)        // Téléporter sans physique
IsGrounded()              // Vérifier si au sol
```

### 4. **PlayerPhysicsComponent** - Contrôleur FPS
Contrôleur de personnage prêt à l'emploi.

**Fichier:** [Components/PlayerPhysicsComponent.cs](Components/PlayerPhysicsComponent.cs)

**Features :**
- Mouvement WASD
- Saut avec Espace
- Mouse look
- Détection au sol
- Gravité

---

## 🚀 Guide de Démarrage Rapide

### Option 1 : Ajouter Physique au Joueur Existant

Ajoutez ces lignes dans **Game.cs** après la création du player :

```csharp
// Dans Game.cs Initialize(), après avoir créé le player

// 1. Ajouter shape (capsule pour FPS)
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetCapsule(0.4f, 1.6f); // radius, height
player.AddComponent(shape);

// 2. Ajouter rigidbody
var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.Mass = 75f;
rb.IsKinematic = true; // Kinematic pour contrôle précis FPS
rb.LockRotationX = true;
rb.LockRotationY = true;
rb.LockRotationZ = true;
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
player.AddComponent(rb);

// 3. (Optionnel) Remplacer PlayerInputComponent par PlayerPhysicsComponent
// player.RemoveComponent<PlayerInputComponent>();
// var physController = new PlayerPhysicsComponent();
// player.AddComponent(physController);
```

### Option 2 : Créer un Nouveau Joueur Physique

```csharp
var player = new GameObject(new Vector3(0, 2, 0));

// Physics shape
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetCapsule(0.4f, 1.6f);
player.AddComponent(shape);

// Rigidbody
var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.IsKinematic = true;
rb.LockRotationX = true;
rb.LockRotationY = true;
rb.LockRotationZ = true;
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
player.AddComponent(rb);

// Controller
var controller = new PlayerPhysicsComponent();
player.AddComponent(controller);

// Camera
var camera = new CameraComponent();
player.AddComponent(camera);
```

---

## 🏗️ Créer des Objets Physiques

### Murs Statiques

```csharp
// Dans Room.cs ou RoomManager.cs
StaticHandle wall = physicsSystem.CreateWall(
    position: new Vector3(10, 2, 0),
    size: new Vector3(0.5f, 4f, 10f) // épaisseur, hauteur, largeur
);

// Stocker le handle pour le cleanup
wallHandles.Add(wall);

// Cleanup
physicsSystem.RemoveStatic(wall);
```

### Plateformes

```csharp
StaticHandle platform = physicsSystem.CreatePlatform(
    position: new Vector3(0, 3, 0),
    size: new Vector3(5f, 0.5f, 5f) // largeur, épaisseur, profondeur
);
```

### Sol

```csharp
StaticHandle floor = physicsSystem.CreateFloor();
```

### Projectiles Dynamiques

```csharp
var projectile = new GameObject(spawnPosition);

// Shape (sphère)
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetSphere(0.2f);
projectile.AddComponent(shape);

// Rigidbody (dynamique)
var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.Mass = 0.05f; // Léger
rb.UseGravity = false; // Pas de gravité pour balle rapide
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
projectile.AddComponent(rb);

// Lancer
rb.SetVelocity(direction * 50f);
```

### Ennemis avec Physique

```csharp
var enemy = new GameObject(spawnPosition);

// Shape (capsule)
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetCapsule(0.3f, 1.2f);
enemy.AddComponent(shape);

// Rigidbody (kinematic pour contrôle IA)
var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.IsKinematic = true;
rb.LockRotationX = true;
rb.LockRotationY = true;
rb.LockRotationZ = true;
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
enemy.AddComponent(rb);
```

---

## 🎯 Cas d'Usage Typiques

### Character Controller (Recommandé pour FPS)

```csharp
// Capsule + Kinematic + Rotations verrouillées
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetCapsule(0.4f, 1.6f);

var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.IsKinematic = true;
rb.LockRotationX = true;
rb.LockRotationY = true;
rb.LockRotationZ = true;
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
```

**Pourquoi ?**
- Capsule : glisse sur les escaliers et bords
- Kinematic : contrôle précis, pas d'effets de physique bizarre
- Rotations verrouillées : pas de culbutes

### Projectile Physique

```csharp
// Sphère + Dynamic + Pas de gravité
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetSphere(0.2f);

var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.Mass = 0.05f;
rb.UseGravity = false;
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());

rb.SetVelocity(direction * speed);
```

### Objet qui Tombe

```csharp
// Box + Dynamic + Gravité
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetBox(new Vector3(1, 1, 1));

var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.Mass = 10f;
rb.UseGravity = true;
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
```

---

## ⚙️ Configuration

### Modifier la Gravité

Dans [Game.cs:110](Game.cs#L110) :
```csharp
physicsSystem.Gravity = new Vector3(0, -20f, 0); // Gravité lunaire
```

### Propriétés des Matériaux

Dans [PhysicsSystem.cs:153](Systems/PhysicsSystem.cs#L153) :
```csharp
pairMaterial = new PairMaterialProperties
{
    FrictionCoefficient = 0.8f, // 0 = glissant, 1 = rugueux
    MaximumRecoveryVelocity = 2f
};
```

---

## ⚡ Performance

**BepuPhysics est optimisé mais gardez en tête :**

- ✅ Bodies statiques : quasi-gratuits
- ✅ Kinematic bodies : très légers
- ⚠️ Dynamic bodies : coûteux si nombreux
- 🎯 Recommandation : <500 bodies dynamiques actifs

**Optimisations :**
- Utilisez `IsKinematic = true` pour objets contrôlés par code
- Désactivez la gravité sur projectiles rapides
- Supprimez les bodies inutilisés avec `Destroy()`

---

## 🔧 API Reference

### PhysicsSystem

```csharp
// Création
AddStaticBox(position, size) → StaticHandle
AddDynamicBody(position, rotation, collidable, inertia, mass, kinematic) → BodyHandle

// Helpers
CreateFloor() → StaticHandle
CreateWall(position, size) → StaticHandle
CreatePlatform(position, size) → StaticHandle

// Suppression
RemoveStatic(handle)
RemoveBody(handle)

// Queries
Raycast(origin, direction, maxDistance, out hit) → bool // TODO
OverlapSphere(center, radius) → bool // TODO

// Advanced
GetBodyReference(handle) → BodyReference
```

### RigidbodyComponent

```csharp
// Configuration
Mass
IsKinematic
UseGravity
LockRotationX/Y/Z

// Méthodes
Initialize(physicsSystem)
CreateBody(shapeIndex, radius)
SetVelocity(velocity)
AddForce(force)
AddImpulse(impulse)
Teleport(position)
IsGrounded() → bool
Destroy()

// Propriétés
LinearVelocity
AngularVelocity
```

### PhysicsShapeComponent

```csharp
Initialize(physicsSystem)
SetSphere(radius)
SetBox(size)
SetCapsule(radius, height)
SetCylinder(radius, height)
GetShapeIndex() → TypedIndex
GetEffectiveRadius() → float
```

---

## ❗ Notes Importantes

1. **Ordre d'ajout :** PhysicsShapeComponent AVANT RigidbodyComponent.CreateBody()
2. **Initialize :** Appeler `.Initialize(physicsSystem)` sur shape et rigidbody
3. **Cleanup :** Appeler `rb.Destroy()` avant de détruire un GameObject
4. **Ancien système :** Vous pouvez garder ColliderComponent pour triggers simples
5. **Raycasting :** Temporairement désactivé (API en cours d'implémentation)

---

## 🐛 Troubleshooting

### "RigidbodyComponent requires PhysicsShapeComponent"
➡️ Ajoutez PhysicsShapeComponent avant d'appeler CreateBody()

### Le corps ne bouge pas
➡️ Vérifiez que `IsKinematic = true` si vous voulez contrôler manuellement
➡️ Ou utilisez `SetVelocity()` / `AddForce()` si dynamique

### Le personnage traverse le sol
➡️ Assurez-vous que le sol est un StaticBody
➡️ Vérifiez que le player a bien un RigidbodyComponent

### Crash au démarrage
➡️ Vérifiez que deltaTime > 0 (déjà fixé normalement)

---

## 📚 Pour Aller Plus Loin

- **Documentation BepuPhysics :** https://github.com/bepu/bepuphysics2
- **Guide de Migration :** [PHYSICS_MIGRATION_GUIDE.md](PHYSICS_MIGRATION_GUIDE.md)
- **Exemples de Code :** Voir [PlayerPhysicsComponent.cs](Components/PlayerPhysicsComponent.cs)

---

## ✅ Checklist de Migration

- [ ] PhysicsSystem ajouté dans Game.cs
- [ ] Player avec PhysicsShapeComponent + RigidbodyComponent
- [ ] Murs convertis en StaticBodies
- [ ] Projectiles avec physique (optionnel)
- [ ] Tests de collision
- [ ] Performance vérifiée (<500 dynamic bodies)

---

**Système prêt à l'emploi ! 🎮**

Le système de physique est maintenant complètement fonctionnel. Vous pouvez :
1. Continuer à utiliser votre système AABB actuel
2. Migrer progressivement vers BepuPhysics
3. Mixer les deux approches

Bon développement ! 🚀
