简介
---
下面的步骤在 Unity 编辑器中执行，用于快速搭建一个可以运行的最小关卡（移动、跳跃、近战、1 个敌人、雨效果）。已在仓库添加了核心脚本到 `Assets/Scripts/`。

快捷步骤（在 Unity 编辑器中）
1. 打开 Unity，载入本项目并保存工作场景的副本：`File -> Save As...` 保存为 `Assets/Scenes/Level_Main.unity`。
2. 在场景中创建地面：
   - 创建一个空的 `GameObject` -> 重命名为 `Ground`。
   - 添加 `SpriteRenderer`（可以先使用内置的 `Default-Particle` 或占位色块）和 `BoxCollider2D`。
   - 将 `BoxCollider2D` 设为合适大小。
3. 创建玩家：
   - 右键 `Hierarchy` -> `2D Object -> Sprite`，重命名为 `Player`。
   - 添加组件：`Rigidbody2D`（Body Type: Dynamic, Freeze Rotation Z），`BoxCollider2D`，`PlayerController`（脚本）。
   - 给 `Player` 添加 `Tag` 为 `Player`（Inspector 顶部 Tag 下拉菜单）。
   - 在 `Player` 下创建空子对象：`GroundCheck`（位置 y:-0.5）和 `AttackPoint`（位置 x:0.6），也可通过 Inspector 把它们拖到 `PlayerController` 的对应字段。
   - 在 `PlayerController` 中设置 `Ground Layer`（例如创建一个 `Ground` layer 并分配给地面 GameObject）。
4. 创建敌人：
   - 新建 `2D Object -> Sprite`，重命名为 `Enemy`。
   - 添加组件：`Rigidbody2D`（Dynamic），`BoxCollider2D`，`SimpleEnemy`（脚本）。
   - 在场景中创建两个空物体 `LeftLimit` 和 `RightLimit` 做为巡逻边界，并在 `SimpleEnemy` 中对其引用。
   - 在 `SimpleEnemy` 的 `Player Layer` 字段中，选择对应的玩家 Layer（或者把玩家设置到某个 Layer 并在此处选择）。
5. 摄像机：
   - 选择 `Main Camera`，添加组件 `CameraFollow`，把 `target` 指向 `Player`。
6. 雨效果（简单）
   - 在场景中创建空对象 `Rain`，添加组件 `ParticleSystem`。
   - 打开 `ParticleSystem` 的 `Renderer`，设置 `Render Mode` 为 `Billboard`，调整 `Start Speed`、`Start Size`、`Emission`、`Shape`（例如 Box）来模拟雨滴。将 `Sorting Layer` 放到背景下。参考 ParticleSystem 的内置示例。
7. GameManager（可选）
   - 创建空对象 `GameManager`，添加组件 `GameManager`（脚本）。
   - 将 `playerPrefab` 指向场景中已配置好的 `Player`（或事先制作成 Prefab），把 `playerSpawn` 指向场景中的一个空对象作为生成点。
8. Layers & Physics
   - 建议创建 Layer：`Ground`、`Player`、`Enemy`。
   - 在 `ProjectSettings -> Physics2D` 中检查碰撞矩阵，确保 `Player` 与 `Ground` 碰撞，`Enemy` 与 `Player` 可触发接触伤害。

   关于复活点与巡逻边界（重要）

   - **复活点（Spawn Point）和巡逻边界（LeftLimit / RightLimit）应为“空对象（Empty）/ Transform”，不要给这些点添加碰撞体（Collider）或刚体（Rigidbody2D）。**
      - 原因：如果复活点或边界是实体（带 Collider 或 Rigidbody），敌人复活或巡逻时会与这些物体发生意外物理碰撞，导致卡住或立刻死亡。
      - 做法：在 Hierarchy 中右键 `Create Empty` 创建空对象，移动到需要位置，然后在 Inspector 中不要添加任何 Collider/Rigidbody，只作为位置引用。

   - 推荐使用 `EnemySpawner` 脚本来管理敌人生成与自动复活：
      1. 在 `Assets/Scripts/` 中有 `EnemySpawner.cs`（已加入仓库）。
      2. 选择你场景中的生成点空对象（或新建空对象 `EnemySpawn`），点击 `添加组件` -> 搜索 `EnemySpawner`。
      3. 先在 Hierarchy 中配置好一个 `Enemy`（包含 `SimpleEnemy`、`BoxCollider2D`、`Rigidbody2D` 等），然后把该 `Enemy` 拖到 Project 面板里创建 Prefab（例如 `Assets/Prefabs/Enemy.prefab`）。
      4. 在 `EnemySpawner` 的 `enemyPrefab` 字段中把上一步创建的 `Enemy` Prefab 拖进去；`spawnPoint` 可保留为空（此组件自身的位置将作为生成点），或拖入你创建的空对象位置。
      5. 设置 `respawnDelay`（例如 2 秒），勾选 `spawnOnStart` 以在场景开始时自动生成敌人。
      6. 运行时：当敌人被击杀（其 `Damageable.Die()` 调用 `Destroy(gameObject)`）后，Spawner 会检测到实例被销毁并在 `respawnDelay` 后重新实例化敌人。

   - 巡逻边界（LeftLimit / RightLimit）用法：
      - 在 `SimpleEnemy` 的 Inspector 中，把 `Left Limit` 与 `Right Limit` 分别指向你场景中的两个空对象（它们只是位置坐标）。
      - 不要把这些边界对象设为可碰撞的实体；如果需要视觉调试，可为它们添加小图标或临时 Sprite，但不添加 Collider。 

   示例快速操作：
   1. Hierarchy -> 右键 -> Create Empty -> 命名为 `EnemySpawn`，放在敌人初始位置。
   2. 在 `EnemySpawn` 上 `Add Component` -> `EnemySpawner`，把 `enemyPrefab` 指向 `Assets/Prefabs/Enemy.prefab`（若还未创建 Prefab，先把场景里的 Enemy 拖到 Project 中创建）。
   3. 在场景中创建 `LeftLimit` 和 `RightLimit` 两个空对象，把它们放在敌人左右边界处，然后把它们分别拖到 `SimpleEnemy` 的对应字段。


按键说明（默认）
- 左右移动：左右箭头 或 `A/D`（取决于 Edit -> Project Settings -> Input 的 `Horizontal`）
- 跳跃：`Space`（`Jump`）
- 攻击：`J`（脚本中为硬编码键位，可改为 InputManager 映射）

提示
- 若没有像素素材，可临时在 `SpriteRenderer` 中使用 `Default` 白色方块并缩放为 32x32。后续可替换为真正的像素图并设置 `Filter Mode = Point`。
- 若希望手柄支持或更复杂输入，可考虑切换到新 Input System（需在 `Packages/manifest.json` 添加 `com.unity.inputsystem`）。

已添加的脚本文件
- [Assets/Scripts/PlayerController.cs](Assets/Scripts/PlayerController.cs#L1)
- [Assets/Scripts/Damageable.cs](Assets/Scripts/Damageable.cs#L1)
- [Assets/Scripts/SimpleEnemy.cs](Assets/Scripts/SimpleEnemy.cs#L1)
- [Assets/Scripts/CameraFollow.cs](Assets/Scripts/CameraFollow.cs#L1)
- [Assets/Scripts/GameManager.cs](Assets/Scripts/GameManager.cs#L1)
