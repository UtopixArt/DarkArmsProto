# Guide de Migration - BepuPhysics v2

Ce guide explique comment migrer votre code existant vers le nouveau système de physique BepuPhysics v2.

## 📦 Composants disponibles

### 1. PhysicsSystem
Le système principal qui gère la simulation physique.
- Déjà intégré dans `Game.cs`
- Update automatique à chaque frame
- Dispose automatique au cleanup

### 2. PhysicsShapeComponent
Définit la forme de collision (à ajouter AVANT le RigidbodyComponent).

```csharp
// Exemple: Capsule pour personnage FPS
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetCapsule(0.4f, 1.6f); // radius, height
gameObject.AddComponent(shape);
```

**Formes disponibles:**
- `SetSphere(radius)` - Pour projectiles
- `SetBox(size)` - Pour objets cubiques
- `SetCapsule(radius, height)` - Pour personnages (RECOMMANDÉ pour FPS)
- `SetCylinder(radius, height)` - Pour objets cylindriques

### 3. RigidbodyComponent
Wrapper pour les corps dynamiques de BepuPhysics.

```csharp
var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.Mass = 75f; // kg
rb.IsKinematic = false; // false = dynamic, true = kinematic
rb.UseGravity = true;
rb.LockRotationX = true; // Verrouiller rotations pour FPS
rb.LockRotationY = true;
rb.LockRotationZ = true;

// Créer le body (APRÈS avoir ajouté PhysicsShapeComponent)
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
gameObject.AddComponent(rb);
```

## 🔄 Migration PlayerInputComponent (Exemple)

### Avant (AABB custom)
```csharp
// Ancien système avec collision AABB manuelle
Vector3 newPosition = Owner.Position + moveDirection * MoveSpeed * deltaTime;
Owner.Position = newPosition;

// Collision manuelle avec chaque mur
foreach (var wall in WallColliders)
{
    if (playerCollider.CheckCollision(wall))
    {
        // Résolution manuelle...
    }
}
```

### Après (BepuPhysics)
```csharp
// Option 1: Character Controller (Kinematic + Raycast)
// RECOMMANDÉ pour FPS avec contrôle précis

public class PlayerPhysicsComponent : Component
{
    private RigidbodyComponent? rb;
    private PhysicsSystem? physics;

    public float MoveSpeed = 10f;
    public float JumpForce = 12f;

    public override void Start()
    {
        physics = /* obtenir référence depuis Game */;
        rb = Owner.GetComponent<RigidbodyComponent>();
    }

    public override void Update(float deltaTime)
    {
        // Mouvement horizontal (kinematic)
        Vector3 moveDir = GetInputDirection();
        Vector3 newPos = Owner.Position + moveDir * MoveSpeed * deltaTime;

        // Vérifier collision avec raycast avant de bouger
        if (!IsColliding(newPos))
        {
            rb?.Teleport(newPos);
        }

        // Saut avec raycast pour détecter le sol
        if (IsGrounded() && Input.Jump)
        {
            rb?.AddImpulse(new Vector3(0, JumpForce, 0));
        }
    }

    private bool IsGrounded()
    {
        return rb?.IsGrounded() ?? false;
    }

    private bool IsColliding(Vector3 position)
    {
        // Raycast dans la direction du mouvement
        Vector3 dir = Vector3.Normalize(position - Owner.Position);
        float dist = Vector3.Distance(Owner.Position, position);
        return physics?.Raycast(Owner.Position, dir, dist, out _) ?? false;
    }
}
```

```csharp
// Option 2: Rigidbody dynamique pur
// Plus "physique", moins de contrôle direct

public override void Update(float deltaTime)
{
    Vector3 moveDir = GetInputDirection();

    // Appliquer force de mouvement
    rb?.SetVelocity(new Vector3(
        moveDir.X * MoveSpeed,
        rb.LinearVelocity.Y, // Garder vélocité verticale
        moveDir.Z * MoveSpeed
    ));

    // Saut
    if (rb?.IsGrounded() == true && Input.Jump)
    {
        rb?.AddImpulse(new Vector3(0, JumpForce, 0));
    }
}
```

## 🎯 Cas d'usage recommandés

### FPS Player Controller
```csharp
// Dans Game.cs Initialize():
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetCapsule(0.4f, 1.6f); // Capsule: meilleur pour escaliers/pentes
player.AddComponent(shape);

var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.Mass = 75f;
rb.IsKinematic = true; // Kinematic pour contrôle précis FPS
rb.LockRotationX = true; // Pas de rotation physique
rb.LockRotationY = true;
rb.LockRotationZ = true;
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
player.AddComponent(rb);
```

### Projectiles
```csharp
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetSphere(0.2f); // Petit rayon pour balle

var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.Mass = 0.05f; // Léger
rb.UseGravity = false; // Pas de gravité pour projectile rapide
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());

// Lancer le projectile
rb.SetVelocity(direction * 50f);
```

### Ennemis
```csharp
var shape = new PhysicsShapeComponent();
shape.Initialize(physicsSystem);
shape.SetCapsule(0.3f, 1.2f);

var rb = new RigidbodyComponent();
rb.Initialize(physicsSystem);
rb.Mass = 50f;
rb.IsKinematic = true; // Contrôle par IA
rb.LockRotationX = true;
rb.LockRotationY = true;
rb.LockRotationZ = true;
rb.CreateBody(shape.GetShapeIndex(), shape.GetEffectiveRadius());
```

## 🛠️ API Rigidbody

```csharp
// Téléportation (sans physique)
rb.Teleport(newPosition);

// Définir vélocité directement
rb.SetVelocity(new Vector3(x, y, z));

// Ajouter force (accélération progressive)
rb.AddForce(new Vector3(0, 100, 0));

// Ajouter impulsion (changement instantané)
rb.AddImpulse(new Vector3(0, 10, 0));

// Check si au sol
bool grounded = rb.IsGrounded();

// Accès à la vélocité actuelle
Vector3 vel = rb.LinearVelocity;
```

## 🏗️ Murs et Plateformes statiques

```csharp
// Dans RoomManager ou Room.cs
public void CreateStaticWalls(PhysicsSystem physics)
{
    // Créer un mur statique
    StaticHandle wall = physics.AddStaticBox(
        position: new Vector3(0, 2, 10),
        size: new Vector3(10, 4, 0.5f) // largeur, hauteur, profondeur
    );

    // Stocker le handle pour pouvoir le supprimer plus tard
    wallHandles.Add(wall);
}

// Cleanup
public void RemoveStaticWalls(PhysicsSystem physics)
{
    foreach (var handle in wallHandles)
    {
        physics.RemoveStatic(handle);
    }
    wallHandles.Clear();
}
```

## 🎮 Raycasting

```csharp
// Exemple: Weapon raycast
Vector3 origin = cameraPosition;
Vector3 direction = cameraForward;
float maxDistance = 100f;

if (physicsSystem.Raycast(origin, direction, maxDistance, out RayHit hit))
{
    Vector3 hitPoint = hit.GetHitPoint(origin, direction);
    Vector3 normal = hit.Normal;

    // Spawner particule d'impact à hitPoint
    // Appliquer dégâts, etc.
}
```

## ⚡ Performance

**BepuPhysics est optimisé mais:**
- Limitez le nombre de bodies dynamiques actifs (<500 recommandé)
- Utilisez `IsKinematic = true` pour objets contrôlés par code
- Les bodies statiques sont quasi-gratuits (utilisez-les!)
- Le threading est géré automatiquement (voir `SimpleThreadDispatcher`)

## 🔧 Configuration avancée

### Modifier la gravité
```csharp
physicsSystem.Gravity = new Vector3(0, -20f, 0); // Gravité lunaire
```

### Propriétés des matériaux
Modifiez `NarrowPhaseCallbacks.ConfigureContactManifold` dans [PhysicsSystem.cs](Systems/PhysicsSystem.cs):
```csharp
pairMaterial.FrictionCoefficient = 0.8f; // 0 = glissant, 1 = rugueux
pairMaterial.MaximumRecoveryVelocity = 2f;
pairMaterial.SpringSettings = new SpringSettings(30, 1);
```

## 📝 Notes importantes

1. **Ordre d'ajout:** Toujours ajouter `PhysicsShapeComponent` AVANT `RigidbodyComponent.CreateBody()`
2. **Initialize:** Appeler `.Initialize(physicsSystem)` sur les deux composants
3. **Kinematic vs Dynamic:**
   - Kinematic = contrôlé par code, pas affecté par forces
   - Dynamic = simulation physique complète
4. **Cleanup:** Appeler `rb.Destroy()` avant de détruire un GameObject
5. **Ancien système:** Vous pouvez garder `ColliderComponent` pour triggers simples

## ✅ Prochaines étapes

1. Migrer `PlayerInputComponent` pour utiliser `RigidbodyComponent`
2. Convertir les murs de `Room.cs` en static bodies
3. Remplacer les collisions projectiles par raycasts
4. Ajouter des effets physiques (ragdolls ennemis?)

---

**Besoin d'aide?** Référez-vous à la doc officielle BepuPhysics: https://github.com/bepu/bepuphysics2
