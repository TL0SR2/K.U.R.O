using Godot;
using System;

namespace Kuros.Core
{
    public partial class GameActor : CharacterBody2D
    {
        [ExportCategory("Stats")]
        [Export] public float Speed = 300.0f;
        [Export] public float AttackDamage = 25.0f;
        [Export] public float AttackRange = 100.0f;
        [Export] public float AttackCooldown = 0.5f;
        [Export] public int MaxHealth = 100;
        
        protected int _currentHealth;
        protected float _attackTimer = 0.0f;
        protected float _hitStunTimer = 0.0f;
        protected bool _facingRight = true;
        protected bool _isPlayingActionAnimation = false;
        
        protected Node2D _spineCharacter;
        protected Sprite2D _sprite;
        protected AnimationPlayer _animationPlayer;

        public override void _Ready()
        {
            _currentHealth = MaxHealth;
            
            // Common node fetching
            _spineCharacter = GetNodeOrNull<Node2D>("SpineCharacter");
            _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            
            if (_spineCharacter != null)
            {
                _animationPlayer = _spineCharacter.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            }
            
            if (_animationPlayer != null)
            {
                _animationPlayer.AnimationFinished += OnAnimationFinished;
                PlayAnimation("animations/Idle");
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_attackTimer > 0) _attackTimer -= (float)delta;
            if (_hitStunTimer > 0) _hitStunTimer -= (float)delta;
        }

        public virtual void TakeDamage(int damage)
        {
            _currentHealth -= damage;
            _currentHealth = Mathf.Max(_currentHealth, 0);
            
            GD.Print($"{Name} took {damage} damage! Health: {_currentHealth}");
            
            // Hit animation & Stun
            if (_animationPlayer != null)
            {
                _isPlayingActionAnimation = true;
                _animationPlayer.Play("animations/hit");
                _hitStunTimer = 0.6f; // Default stun
                GD.Print($"[{Name} Animation] Playing hit animation");
            }

            FlashDamageEffect();

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            // Override in subclasses
            QueueFree();
        }

        protected virtual void FlashDamageEffect()
        {
            Node2D visualNode = _spineCharacter ?? (Node2D)_sprite;
            if (visualNode != null)
            {
                var originalColor = visualNode.Modulate;
                visualNode.Modulate = new Color(1, 0, 0); // Default Red
                
                var tween = CreateTween();
                tween.TweenInterval(0.1);
                tween.TweenCallback(Callable.From(() => visualNode.Modulate = originalColor));
            }
        }

        protected void FlipFacing(bool faceRight)
        {
            if (_facingRight == faceRight) return;
            
            _facingRight = faceRight;
            
            if (_spineCharacter != null)
            {
                var scale = _spineCharacter.Scale;
                // Set absolute X scale based on direction
                float absX = Mathf.Abs(scale.X);
                _spineCharacter.Scale = new Vector2(faceRight ? absX : -absX, scale.Y);
            }
            else if (_sprite != null)
            {
                _sprite.FlipH = !_facingRight;
            }
        }

        protected void PlayAnimation(string animName)
        {
            if (_animationPlayer == null) return;
            
            // Safe play method if needed, but usually we access _animationPlayer directly or use specific logic
            _animationPlayer.Play(animName);
        }

        protected virtual void OnAnimationFinished(StringName animName)
        {
            GD.Print($"[{Name} Animation] OnAnimationFinished: {animName}");
            
            if (animName == "animations/attack" || animName == "animations/hit")
            {
                if (_animationPlayer != null)
                {
                    GD.Print($"[{Name} Animation] Playing RESET to restore bones");
                    _animationPlayer.Play("RESET");
                }
            }
            else if (animName == "RESET")
            {
                if (_animationPlayer != null)
                {
                    GD.Print($"[{Name} Animation] RESET complete, playing Idle");
                    _animationPlayer.Play("animations/Idle");
                    _isPlayingActionAnimation = false;
                    GD.Print($"[{Name} Animation] Animation lock released");
                }
            }
        }
        
        protected void ClampPositionToScreen(float margin = 50f, float bottomOffset = 150f)
        {
             var screenSize = GetViewportRect().Size;
             GlobalPosition = new Vector2(
                Mathf.Clamp(GlobalPosition.X, margin, screenSize.X - margin),
                Mathf.Clamp(GlobalPosition.Y, margin, screenSize.Y - bottomOffset)
            );
        }
    }
}

