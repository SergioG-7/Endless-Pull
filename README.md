# ⚔️ Endless Pull

[Español](#-español) | [English](#-english) | [日本語](#-日本語)

---

## 🇪🇸 Español

**Endless Pull** es un RPG táctico gacha 2D y simulador de gestión de base inspirado en la estética y mecánicas de obras de fantasía oscura y manhwas tácticos (como *Pick Me Up*). Desarrollado en Unity (C#), el proyecto combina combate en tiempo real por escuadras, toma de decisiones estratégicas, economía persistente y experimentación con Inteligencia Artificial por Refuerzo (Unity ML-Agents).

### 🎮 Jugar la Demo
* Jugar en Itch.io (PC Windows / WebGL) - [https://sergiog-7.itch.io/endless-pull] (contraseña: level5)
* Ver gameplay en YouTube - [https://youtu.be/placeholder-gameplay]
* Ver combate y ML-Agents Gym en YouTube - [https://youtu.be/placeholder-gym]

### 🛠️ Tecnologías y Herramientas
* **Motor Core:** Unity (C#), uGUI, TextMeshPro
* **IA & Simulación:** Unity ML-Agents (Reinforcement Learning), Finite State Machines (FSM)
* **Patrones:** Arquitectura Data-Driven (ScriptableObjects), State Machine (FSM), Observer / Event-Driven, Object Pooling, Persistencia JSON.

### 🚀 Desafíos Técnicos Resueltos (Highlights)
* **Arquitectura RPG Compleja, Simulación de Base y Diseño Data-Driven:** Sistema escalable de 39 héroes únicos y 18 subclases especializadas a partir de 3★ con habilidades activas diferenciadas, maestrías de armas y rasgos de personalidad. Simulación autónoma de base con asignación de trabajadores a instalaciones (Granja, Cantina, Taller, Campo de Entrenamiento), desgaste y forja de equipamiento, sinergias de escuadra y jerarquía social inspirada en manhwas tácticos.
* **Combate Táctico en Tiempo Real, Aggro Dinámico y Gimnasio ML-Agents:** Sistema de combate por escuadras con gestión de amenaza (*aggro*) limitada en tanques (máximo 2 enemigos para permitir flanqueos a la retaguardia), IA de curanderos prioritaria por porcentaje de salud, estados alterados (veneno, aturdimiento, escudos) y avisos de peligro en suelo (*telegraphing* de jefes). Incluye una arena desacoplada (`Gym_Combat`) con simulación acelerada ($10\times$), vector de 8 observaciones normalizadas y funciones de recompensa para entrenamiento de agentes por refuerzo.
* **Sistema de Localización CJK Dinámico y UI Ergonómica Multiplataforma:** Gestor de localización trilingüe (Español, Inglés y Japonés) integrado con fuentes CJK dinámicas mediante *Fallback Font Assets* en TextMeshPro para garantizar renderizado nítido sin caracteres rotos (*tofu*). Interfaz *Dark Glassmorphism* optimizada para dispositivos móviles (cajón lateral retráctil, TopBar con soporte de *Safe Area*, Roster modular en tarjetas e inspección interactiva del mundo mediante Raycast 2D).

---

## 🇬🇧 English

**Endless Pull** is a 2D tactical gacha RPG and fortress management simulator inspired by dark fantasy tactical manhwas (such as *Pick Me Up*). Developed in Unity (C#), the project combines real-time role-based squad combat, strategic resource management, persistent economy, and Reinforcement Learning AI experimentation (Unity ML-Agents).

### 🎮 Play the Demo
* Play on Itch.io (PC Windows / WebGL) - [https://sergiog-7.itch.io/endless-pull] (password: level5)
* Watch gameplay on YouTube - [https://youtu.be/placeholder-gameplay]
* Watch combat & ML-Agents Gym on YouTube - [https://youtu.be/placeholder-gym]

### 🛠️ Technologies & Tools
* **Core Engine:** Unity (C#), uGUI, TextMeshPro
* **AI & Simulation:** Unity ML-Agents (Reinforcement Learning), Finite State Machines (FSM)
* **Patterns:** Data-Driven Architecture (ScriptableObjects), State Machine (FSM), Observer / Event-Driven, Object Pooling, JSON Persistence.

### 🚀 Technical Highlights
* **Complex RPG Architecture, Base Simulation & Data-Driven Design:** Scalable system featuring 39 unique heroes and 18 specialized subclasses from 3★ upwards with dedicated active skills, weapon masteries, and personality traits. Autonomous base simulation featuring facility worker assignments (Farm, Canteen, Workshop, Training Grounds), equipment durability and crafting, squad origin synergies, and a tactical manhwa-inspired social hierarchy.
* **Real-Time Tactical Combat, Dynamic Aggro & ML-Agents Gym:** Squad combat system featuring limited tank threat management (capping at 2 enemies to allow tactical flanking to the backline), health-percentage priority support/healer AI, status effects (poison, stun, shields), and ground area telegraph warnings. Includes a standalone high-speed training arena (`Gym_Combat`, $10\times$ timescale) with an 8-observation normalized vector and shaped reward functions for Reinforcement Learning agents.
* **Dynamic CJK Localization System & High-Performance Mobile UI:** Trilingual modular localization (Spanish, English, Japanese) integrated with dynamic CJK font assets using TextMeshPro Fallbacks to ensure crisp rendering with zero missing glyphs. Sleek *Dark Glassmorphism* interface tailored for mobile devices (collapsible drawer navigation, safe-area resource top bar, modular card roster, and seamless 2D raycast world inspection).

---

## 🇯🇵 日本語

**Endless Pull（エンドレス・プル）**は、ダークファンタジーや戦術系マンガ（『Pick Me Up』等）の世界観に着想を得た、2DタクティカルガチャRPGおよび拠点経営シミュレーションゲームです。Unity（C#）で開発され、リアルタイムの役割ベース部隊戦闘、戦略的なリソース管理、永続的な経済システム、そして強化学習AI（Unity ML-Agents）の実験的導入を統合しています。

### 🎮 デモをプレイする
* Itch.ioでプレイ (PC Windows / WebGL) - [https://sergiog-7.itch.io/endless-pull] (パスワード：level5)
* YouTubeでゲームプレイを見る - [https://youtu.be/placeholder-gameplay]
* YouTubeで戦闘＆ML-Agentsジムを見る - [https://youtu.be/placeholder-gym]

### 🛠️ 使用技術とツール
* **コアエンジン:** Unity (C#), uGUI, TextMeshPro
* **AI・シミュレーション:** Unity ML-Agents（強化学習）、有限ステートマシン (FSM)
* **設計パターン:** データドリブン・アーキテクチャ (ScriptableObjects)、ステートマシン (FSM)、Observer / イベント駆動、オブジェクトプーリング、JSON永続化

### 🚀 主な技術的ハイライト
* **複雑なRPGアーキテクチャ・拠点シミュレーション・データドリブン設計:** 39名のユニークな英雄と、★3から分岐する18種類の専門上位クラス（固有アクティブスキル、武器熟練度、性格特性付き）を実装。施設への人員配置（農場、酒場、工房、訓練所）、装備の耐久度・鍛造、部隊シナジー、マンガに着想を得た序列システムを備えた自律的な拠点シミュレーション。
* **リアルタイム戦術戦闘・動的ヘイト管理・ML-Agentsジム:** タンクのヘイト上限管理（後衛への回り込みを可能にする最大2体制限）、残HP比率を優先する支援・ヒーラーAI、状態異常（毒、スタン、シールド）、ボスの床面攻撃予兆（テレグラフ）を備えた部隊戦闘システム。8次元の正規化観測ベクトルと強化学習エージェント用の報酬設計を備えた、10倍速シミュレーション可能な独立訓練アリーナ（`Gym_Combat`）を構築。
* **動的CJKローカライゼーションシステムと高パフォーマンスなモバイルUI:** TextMeshProのフォールバックアセットを利用した動的CJKフォント統合による、文字化け（トーフ）のないスペイン語・英語・日本語の3言語ローカライゼーション。モバイル向けに最適化されたダークグラスモーフィズムUI（折りたたみ式ドロワー、セーフエリア対応リソースバー、カード型ロスター、2Dレイキャストによる拠点・英雄の直感的なワールドインスペクション）。
