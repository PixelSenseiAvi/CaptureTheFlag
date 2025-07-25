# Training Parameters Analysis - CTF Phase 1

## Updated `train_1.yaml` Configuration Analysis

### 🔧 **Core PPO Hyperparameters**

| Parameter | Original | Updated | Justification |
|-----------|----------|---------|---------------|
| `batch_size` | 1024 | **2048** | Larger batches provide more stable gradient updates with 2 competing agents |
| `buffer_size` | 10240 | **20480** | 10x batch_size ratio ensures good sample diversity and prevents overfitting |
| `learning_rate` | 3.0e-4 | **3.0e-4** | ✅ Good starting point for PPO |
| `beta` | 5.0e-4 | **1.0e-3** | Higher entropy bonus encourages exploration in competitive environment |
| `epsilon` | 0.2 | **0.2** | ✅ Standard PPO clipping parameter |
| `lambd` | 0.95 | **0.95** | ✅ Good GAE parameter for advantage estimation |

### 🧠 **Network Architecture**

| Parameter | Original | Updated | Reasoning |
|-----------|----------|---------|-----------|
| `hidden_units` | 128 | **256** | More capacity needed for 36 observations and competitive strategy |
| `num_layers` | 2 | **3** | Additional depth helps learn complex spatial relationships |
| `normalize` | true | **true** | ✅ Essential for diverse observation ranges (-20 to +20, 0-1, etc.) |

### ⏱️ **Training Schedule**

| Parameter | Original | Updated | Benefits |
|-----------|----------|---------|---------|
| `max_steps` | 500K | **1M** | More training time for convergence in competitive scenario |
| `time_horizon` | 64 | **128** | Longer episodes allow for more strategic planning |
| `summary_freq` | 10000 | **5000** | More frequent monitoring of training progress |

### 🏆 **Self-Play Configuration** (NEW)

```yaml
self_play:
  save_steps: 50000              # Save policy every 50k steps
  team_change: 200000            # Switch teams every 200k steps
  swap_steps: 25000              # Change opponent every 25k steps  
  window: 10                     # Keep 10 previous policies
  play_against_latest_model_ratio: 0.5  # Mix of latest vs historical opponents
  initial_elo: 1200              # Starting ELO rating
```

**Why Self-Play?**
- **Prevents Overfitting**: Agents won't exploit specific opponent weaknesses
- **Continuous Challenge**: Always training against improving opponents
- **Diverse Strategies**: Exposure to different play styles from policy history
- **ELO Tracking**: Monitor relative skill progression

### 🎯 **Reward Signal Configuration**

| Parameter | Value | Purpose |
|-----------|-------|---------|
| `gamma` | 0.99 | High discount factor encourages long-term strategic thinking |
| `strength` | 1.0 | Full weight on extrinsic rewards (flag capture, scoring) |

### 📊 **Expected Training Performance**

**Timeline Estimate:**
- **0-100K steps**: Basic movement and flag interaction
- **100K-300K steps**: Strategic positioning and flag capture tactics  
- **300K-600K steps**: Counter-strategies and defensive play
- **600K-1M steps**: Advanced competitive strategies

**Key Metrics to Monitor:**
- **Episode Length**: Should increase as agents get better at defense
- **Flag Captures per Episode**: Initially high, should balance out
- **Score Distribution**: Should become more balanced between teams
- **ELO Progression**: Steady increase indicates learning

### ⚙️ **Training Command**

```bash
# Basic training
mlagents-learn Assets/Scripts/train_1.yaml --run-id=CTF_Phase1_v1

# With TensorBoard monitoring
mlagents-learn Assets/Scripts/train_1.yaml --run-id=CTF_Phase1_v1 --force

# Resume training from checkpoint
mlagents-learn Assets/Scripts/train_1.yaml --run-id=CTF_Phase1_v1 --resume
```

### 🔍 **Monitoring Tips**

**TensorBoard Metrics to Watch:**
- `Policy/Extrinsic Reward`: Should trend upward
- `Environment/Cumulative Reward`: Team performance balance
- `Policy/Entropy`: Should start high, gradually decrease
- `Policy/Learning Rate`: Should decrease linearly to 0

**Warning Signs:**
- **Reward Plateau**: May need learning rate adjustment
- **High Entropy**: Agents not converging - reduce beta
- **Low Entropy**: Agents converging too fast - increase beta
- **Unstable Training**: Reduce learning rate or batch size

### 🚀 **Performance Optimizations**

**For Faster Training:**
- Increase `num_envs` in training (multiple parallel environments)
- Use GPU acceleration if available
- Reduce `summary_freq` during stable training phases

**For Better Results:**
- Tune `time_horizon` based on episode length
- Adjust `batch_size` based on available memory
- Fine-tune reward values in agent code if needed

This configuration is optimized for competitive multi-agent training with self-play, providing a robust foundation for learning complex CTF strategies! 