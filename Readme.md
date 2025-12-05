# Dark Arms Prototype - Raylib C#

FPS Roguelike avec système d'armes organiques évolutives, inspiré de Dark Arms (Neo Geo Pocket) et Binding of Isaac.

## Setup
```bash
# Restore dependencies
dotnet restore

# Run the game
dotnet run

# Build release
dotnet build -c Release
```

## Contrôles

- **WASD** - Déplacement
- **Souris** - Viser
- **Clic gauche** - Tirer
- **E** - Évoluer l'arme (quand disponible)
- **ESC** - Quitter

## Architecture
```
DarkArmsProto/
├── Program.cs              # Entry point
├── Game.cs                 # Game loop principal
├── Player/
│   └── PlayerController.cs # FPS controller avec screen shake
├── Weapons/
│   ├── WeaponSystem.cs     # Système d'évolution
│   └── Projectile.cs       # Projectiles avec homing
├── Enemies/
│   ├── Enemy.cs            # Ennemis avec hit feedback
│   └── EnemySpawner.cs     # Spawn system
└── Souls/
    ├── Soul.cs             # Pickups avec magnétisme
    └── SoulManager.cs      # Collection system
```

## Système d'évolution

### Types d'âmes
- **Beast** (Orange) - Vitesse et instinct
- **Undead** (Vert) - Drain de vie et régénération
- **Demon** (Rouge) - Dégâts rapides et homing

### Armes Stage 1
- **Flesh Pistol** (base) - Tir équilibré standard
- Collecte 10 souls → Évolution disponible

### Armes Stage 2 (10 souls)
- **Bone Revolver** (Beast) - Gros projectile orange, piercing, dégâts élevés
- **Tendril Burst** (Undead) - Shotgun 5 projectiles, lifesteal actif
- **Parasite Swarm** (Demon) - Projectiles rapides avec homing automatique

### Armes Stage 3 (25 souls)
- **Apex Predator** (Beast) - Dégâts massifs, gros projectiles
- **Necrotic Cannon** (Undead) - Shotgun amélioré, lifesteal x2
- **Inferno Beast** (Demon) - Tir ultra-rapide avec homing

## Features implémentées

### ✅ Gameplay Core
- FPS controller fluide avec mouvement WASD
- Système de tir avec projectiles physiques
- 3 types d'ennemis avec AI (chase player)
- Collection d'âmes avec magnétisme
- Évolution d'arme basée sur âme dominante
- Comportements d'armes différenciés (piercing, shotgun, homing)

### ✅ Visual Feedback
- **Screen shake** sur tir
- **Hit flash** blanc sur ennemis touchés
- **Damage numbers** flottants en 3D
- **Health bars** dynamiques sur ennemis
- **Glow effects** sur projectiles et souls
- **UI complète** avec barres d'âmes par type

### ✅ Weapon Mechanics
- **Piercing** - Les projectiles traversent les ennemis (Bone Revolver)
- **Lifesteal** - Récupère HP sur hit (Tendril Burst)
- **Homing** - Projectiles suivent les ennemis (Parasite Swarm)
- **Shotgun spread** - 5 projectiles en éventail

## Gameplay Loop

1. **Kill enemies** → Drop souls colorées selon type
2. **Collect souls** → Magnétisme automatique près du joueur
3. **Feed weapon** → Les âmes nourrissent l'arme organique
4. **Evolution** → Type d'âme dominant détermine l'évolution
5. **New abilities** → Comportement de tir change radicalement

## Tweaking rapide

### WeaponSystem.cs
```csharp
private int[] requiredSouls = { 10, 25, 50 };  // Seuils d'évolution
private float damage = 20f;                     // Dégâts de base
private float fireRate = 3f;                    // Tirs par seconde
```

### PlayerController.cs
```csharp
private float moveSpeed = 5f;           // Vitesse de déplacement
private float mouseSensitivity = 0.003f; // Sensibilité souris
```

### Enemy.cs
```csharp
// Dans le constructor, par type:
maxHealth = 30f;    // HP Beast
maxHealth = 50f;    // HP Undead
maxHealth = 40f;    // HP Demon
speed = 3f;         // Vitesse de chase
```

### Soul.cs
```csharp
private float magnetRadius = 3f;   // Rayon d'attraction
private float collectRadius = 1.5f; // Rayon de collection
```

## Prochaines étapes

### 🔥 High Priority
- [ ] **Génération procédurale de rooms** (type Binding of Isaac)
- [ ] **Boss fights** avec patterns d'attaque
- [ ] **Plus de types d'ennemis** (shooters, chargers, tanks)
- [ ] **Sound effects** (tir, hit, soul collect, evolution)
- [ ] **Particle systems** (muzzle flash, blood, explosions)

### 🎨 Polish
- [ ] Weapon model visible (main gauche avec animation)
- [ ] Evolution animation (transformation visuelle)
- [ ] Blood splatter decals
- [ ] Mini-map avec fog of war
- [ ] Menu pause / game over avec retry
- [ ] Camera FOV kick sur tir

### 🚀 Gameplay
- [ ] **Items passifs** (type Binding of Isaac) qui se stackent
- [ ] **Mutations visuelles** des armes selon souls absorbées
- [ ] Patterns d'attaque ennemis (projectiles, dash)
- [ ] Treasure rooms, shops, sacrifice rooms
- [ ] Meta-progression (unlocks permanents)
- [ ] Multiple floors avec difficulté croissante

### ⚡ Optimizations
- [ ] Object pooling pour projectiles
- [ ] Spatial partitioning pour collisions
- [ ] LOD pour ennemis distants
- [ ] Refactor: extraire UIManager, CollisionSystem

## Notes techniques

### Raylib-cs 7.0 API
Le projet utilise Raylib-cs 7.0 avec les conventions suivantes :
- `Color.White` au lieu de `Color.WHITE`
- `KeyboardKey.W` au lieu de `KEY_W`
- `MouseButton.Left` au lieu de `MOUSE_BUTTON_LEFT`
- Propriétés Color en majuscules: `color.R, color.G, color.B`

### Port vers Unity
Cette architecture se transfère facilement :
- `PlayerController` → MonoBehaviour + CharacterController
- `WeaponSystem` → ScriptableObject pour data-driven design
- `Enemy` → NavMeshAgent pour pathfinding
- `Projectile` → Rigidbody avec collisions physiques
- Game loop reste identique (Update/Render → Update/LateUpdate)

## Design Philosophy

**L'évolution automatique basée sur l'âme dominante** (pas de menu de choix) encourage :
- 🎯 **Ciblage tactique** - Choisir quels ennemis tuer
- 📊 **Planification de build** - Viser un type d'âme spécifique
- 🔄 **Rejouabilité** - Chaque run est différente selon les spawns
- ⚡ **Pace rapide** - Pas de pause pour menu, action continue

**Alternative possible** : Menu de choix entre 2-3 évolutions (comme dans le proto Three.js) pour plus de contrôle joueur.

## Crédits

Inspiré de :
- **Dark Arms: Beast Buster 1999** (Neo Geo Pocket Color) - Système d'armes organiques
- **The Binding of Isaac** - Structure roguelike et génération de rooms
- **Vampire Survivors** - Évolutions d'armes et builds synergiques