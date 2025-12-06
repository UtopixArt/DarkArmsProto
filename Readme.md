# 🎮 Dark Arms Prototype - FPS Roguelike

> Inspiré de **Dark Arms: Beast Buster 1999** (Neo Geo Pocket) × **The Binding of Isaac** × **Vampire Survivors**

FPS roguelike avec système d'armes organiques évolutives basé sur l'absorption d'âmes. Construit en C# avec Raylib, architecture data-driven propre.

---

## 🚀 Quick Start

```bash
# Restore dependencies
dotnet restore

# Run the game
dotnet run

# Build release
dotnet build -c Release
```

**Prérequis:** .NET 8.0 SDK

---

## 🎯 Contrôles

| Action | Contrôle |
|--------|----------|
| Déplacement | `WASD` |
| Viser | Souris |
| Tirer | Clic gauche |
| Évoluer l'arme | `E` (quand disponible) |
| Éditeur de salle | `F1` |
| Toggle Colliders | `F3` |
| Quitter | `ESC` |

---

## 🏗️ Architecture

### 📂 Structure du Projet

```
DarkArmsProto/
├── src/
│   ├── Audio/                  # AudioManager (sons procéduraux)
│   ├── Components/             # Composants ECS
│   │   ├── AI/                 # State Machine ennemis
│   │   │   ├── IEnemyState.cs
│   │   │   └── EnemyStates.cs  # Idle, Wander, Chase, Attack, Cooldown
│   │   ├── CameraComponent.cs
│   │   ├── ColliderComponent.cs (AABB)
│   │   ├── EnemyAIComponent.cs
│   │   ├── HealthComponent.cs
│   │   ├── ProjectileComponent.cs
│   │   ├── WeaponComponent.cs   # ⭐ Data-driven weapon system
│   │   └── ...
│   ├── Core/                   # GameObject, Component, SoulType
│   ├── Data/                   # ⭐ JSON Data Classes + Databases
│   │   ├── WeaponData.cs
│   │   ├── WeaponDatabase.cs
│   │   ├── EnemyData.cs
│   │   └── EnemyDatabase.cs
│   ├── Enemies/                # EnemySpawner (Factory)
│   ├── Factories/              # ⭐ ProjectileFactory
│   ├── Souls/                  # SoulManager
│   ├── Systems/                # ⭐ Systèmes refactorisés
│   │   ├── CombatSystem.cs     # Collisions, dégâts, morts
│   │   ├── ProjectileSystem.cs # Gestion projectiles
│   │   ├── GameUI.cs           # UI 2D/3D
│   │   └── MapEditor.cs        # Éditeur in-game
│   ├── VFX/                    # ParticleManager, LightManager
│   ├── World/                  # RoomManager, Room, Door
│   ├── Game.cs                 # Game loop principal
│   ├── GameConfig.cs           # ⭐ Configuration centralisée
│   └── Program.cs              # Entry point
│
├── resources/
│   ├── data/
│   │   ├── weapons.json        # ⭐ 13 armes configurées
│   │   └── enemies.json        # ⭐ 3 types d'ennemis
│   ├── rooms/                  # Templates JSON (éditeur)
│   └── shaders/                # lighting.vs/fs (éclairage dynamique)
│
├── DarkArmsProto.csproj
├── DarkArmsProto.sln
└── README.md
```

---

## 🧩 Design Patterns Implémentés

### ✅ **Data-Driven Design**
- **Armes** configurées via `weapons.json` (13 armes, 5 stages)
- **Ennemis** configurés via `enemies.json` (Beast, Undead, Demon)
- Modification du gameplay sans recompilation

### ✅ **Factory Pattern**
- `ProjectileFactory`: Création centralisée des projectiles
- `EnemySpawner`: Instanciation des ennemis depuis JSON
- Suppression de duplication massive de code

### ✅ **State Pattern**
- AI ennemis avec FSM (Idle → Wander → Chase → Attack → Cooldown)
- Comportements modulaires et extensibles

### ✅ **Component Pattern (ECS-lite)**
- `GameObject` = conteneur de `Component`
- Séparation claire data/logique/rendering
- Ajout de features sans modifier classes existantes

### ✅ **Observer Pattern**
- Events: `OnDamageTaken`, `OnShoot`, `OnExplosion`
- Découplage entre systèmes

### ✅ **Singleton Pattern**
- `AudioManager.Instance`
- `WeaponDatabase.Load()` / `EnemyDatabase.Load()`

---

## 🔫 Système d'Évolution des Armes

### Types d'Âmes
| Âme | Couleur | Archétype |
|-----|---------|-----------|
| **Beast** | 🟠 Orange | Vitesse, instinct, DPS |
| **Undead** | 🟢 Vert | Lifesteal, zone damage |
| **Demon** | 🔴 Rouge | Homing, explosif |

### Progression (5 Stages)

#### **Stage 1** - Arme de base
- `Flesh Pistol`: Tir standard (10 âmes → Stage 2)

#### **Stage 2** (10 âmes)
- `Bone Revolver` (Beast): SMG rapide, piercing
- `Tendril Burst` (Undead): Shotgun 6 projectiles + lifesteal
- `Parasite Swarm` (Demon): 3 projectiles homing

#### **Stage 3** (25 âmes)
- `Apex Predator` (Beast): Minigun haute cadence
- `Necrotic Cannon` (Undead): Grenade explosive (AOE)
- `Inferno Beast` (Demon): Railgun perforant

#### **Stage 4** (50 âmes)
- `Feral Shredder` (Beast): Double projectiles piercing
- `Plague Spreader` (Undead): 8 grenades explosives
- `Hellfire Missiles` (Demon): 4 missiles homing explosifs

#### **Stage 5** (100 âmes)
- `Omega Fang` (Beast): Triple minigun + lifesteal
- `Death's Hand` (Undead): 12 projectiles massifs
- `Armageddon` (Demon): Nuke (20x dégâts, 15m radius)

---

## 👾 Ennemis

| Type | HP | Speed | Behavior | Abilities |
|------|----|----|----------|-----------|
| **Beast** | 50 | 8.0 | Melee Rusher | Charge rapide, recul après attaque |
| **Undead** | 80 | 3.0 | Ranged Tank | Projectiles poison (vert) |
| **Demon** | 65 | 6.0 | Flying Striker | Vol, projectiles rapides (rouge) |

**IA:** State Machine avec détection, chase, attaque, cooldown, esquive

---

## 🗺️ Génération Procédurale

### Donjon
- **15 salles max** connectées en grille
- Types: Start, Normal, Boss, Treasure, Shop
- Génération récursive avec densité 75%

### Salles
- **Layouts procéduraux**: Random Blocks, Catwalks, Split-Level, Arena
- **Templates JSON**: Éditables via l'éditeur in-game (F1)
- **Platforming 3D**: Escaliers, passerelles, multi-niveaux

### Éditeur de Salle (F1)
- **Outils**: Platform (1), Spawner (2), Light (3)
- **Contrôles**: Flèches/R/F (resize), Click (placer), Del (clear)
- **Fichiers**: `[`/`]` (changer fichier), F5 (save), F6 (load)
- **Format**: `resources/rooms/room_XX.json`

---

## 💡 Systèmes VFX

### Éclairage Dynamique
- **Shader GLSL** custom (lighting.vs/fs)
- **32 lumières dynamiques** max
- Types: Muzzle flash, Impact, Explosion, Static (salles)
- **HDR-like** avec intensité > 1.0

### Particules
- **Explosion**: 40 particules avec gravité
- **Impact**: 10 particules directionnelles
- **Soul Collect**: 12 particules radiales
- **Muzzle Flash**: 2 particules courtes

### Screen Shake
- **Trauma-based** (Squirrel Eiserloh method)
- Décroissance smooth avec shake²
- Intensité configurable par événement

---

## ⚙️ Configuration Gameplay

### `GameConfig.cs` - Tweaking Centralisé

```csharp
// Player
public const float PlayerMoveSpeed = 10f;
public const float PlayerMaxHealth = 100f;

// Weapons
public const float BaseDamage = 20f;
public const float BaseFireRate = 3f;
public const int RequiredSoulsStage2 = 10;

// Enemies
public const float BeastEnemySpeed = 8.0f;
public const float DemonEnemyHealth = 65f;
public const float EnemyTouchDamagePerSecond = 15f;

// World
public const float RoomSize = 30f;
public const float WallHeight = 15f;
```

**Modification rapide** sans plonger dans le code.

---

## 🎨 Features Visuelles

### ✅ Implémenté
- Screen shake sur tir/kill
- Hit flash blanc sur ennemis
- Damage numbers flottants 3D
- Health bars dynamiques
- Glow effects sur projectiles/âmes
- UI complète (stats, armes, minimap)
- Collider debug wireframes (F3)
- Éclairage volumétrique shader

### 🔄 Prochaines Étapes
- [ ] Weapon model visible (main gauche)
- [ ] Evolution animation (transformation)
- [ ] Blood splatter decals
- [ ] Sound effects (tir, hit, collect)
- [ ] Boss patterns d'attaque
- [ ] Plus de types d'ennemis (shooter, charger, tank)

---

## 🔧 Refactoring Réalisé

### Avant → Après

| Fichier | Avant | Après | Réduction |
|---------|-------|-------|-----------|
| `WeaponSystem.cs` | 300+ lignes | **Supprimé** | -100% |
| `WeaponComponent.cs` | 700 lignes | 150 lignes | **-78%** |
| `EnemySpawner.cs` | 200 lignes | 60 lignes | **-70%** |

### Patterns Appliqués
1. ✅ **Data-Driven**: JSON configs + Database classes
2. ✅ **Factory**: ProjectileFactory, EnemySpawner
3. ✅ **State Machine**: EnemyAI states modulaires
4. ✅ **Component**: Séparation claire concerns
5. ✅ **Observer**: Events pour découplage

### Code Supprimé
- ❌ 5000+ lignes de duplication (if/else géants)
- ❌ Hardcoded weapon stats
- ❌ Hardcoded enemy stats
- ❌ WeaponSystem.cs redondant

---

## 🎓 Prochains Design Patterns à Explorer

### **Strategy Pattern**
- Armes avec différentes stratégies de tir
- Swappable attack behaviors

### **Object Pool Pattern**
- Pool de projectiles/particules
- Optimisation garbage collection

### **Command Pattern**
- Input buffering
- Undo/Redo pour éditeur

### **Builder Pattern**
- Construction complexe de rooms/weapons
- Fluent API pour configuration

### **Decorator Pattern**
- Power-ups qui modifient comportement armes
- Buffs/debuffs empilables

---

## 📊 Métriques Techniques

- **ECS Architecture**: GameObject + Component modulaire
- **Collision**: AABB (Box colliders) avec sliding
- **Physics**: Gravité, jump, projectile ballistics
- **Rendering**: Raylib 3D + custom lighting shader
- **JSON**: System.Text.Json avec snake_case support
- **Platform**: .NET 8.0, cross-platform

---

## 📝 Notes Techniques

### Raylib-cs 7.0
- API moderne C# bindings
- Pas de dépendance Unity/Godot
- Léger, performant, contrôle total

### Architecture ECS-lite
- Custom implementation (pas de framework externe)
- `GameObject` = Position + List<Component>
- `Component` = Start/Update/Render hooks
- Facile à étendre, debugging simple

### JSON Deserialization
```csharp
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
};
```
**Problème résolu:** snake_case JSON → PascalCase C#

---

## 🎯 Roadmap

### High Priority
- [ ] **Plus de room templates** (10+ variantes)
- [ ] **Boss fights** avec patterns dédiés
- [ ] **Sound effects** (tir, hit, collect, evolution)
- [ ] **4+ nouveaux ennemis** (Shooter, Tank, Charger, Elite)

### Polish
- [ ] Weapon model visible
- [ ] Evolution animation (VFX transformation)
- [ ] Minimap fog of war
- [ ] Blood splatter decals
- [ ] Menu principal + pause

### Systems
- [ ] Power-ups (speed, damage, shield)
- [ ] Stat system (upgrades permanents)
- [ ] Meta-progression (unlocks)
- [ ] Leaderboard/scoring

---

## 🙏 Crédits

**Inspirations:**
- Dark Arms: Beast Buster 1999 (SNK)
- The Binding of Isaac (Edmund McMillen)
- Vampire Survivors (poncle)

**Tech Stack:**
- Raylib-cs 7.0
- .NET 8.0
- System.Text.Json

---

## 📜 License

Prototype personnel - Code éducatif

---

**Construit par Valentin** 🎮