using Godot;
using System;
using Kuros.Core;

public partial class SamplePlayer : GameActor
{
    private Area2D _attackArea = null!;
    private Label _statsLabel = null!;
    private int _score = 0;
    private ColorRect _attackVisualization = null!;
    
    public override void _Ready()
    {
        // Initialize base (stats, nodes, animations)
        base._Ready();
        
        _attackArea = GetNode<Area2D>("AttackArea");
        _statsLabel = GetNode<Label>("../UI/PlayerStats");
        
        UpdateStatsUI();
        
        // Create attack visualization
        _attackVisualization = new ColorRect();
        _attackVisualization.Size = new Vector2(80, 60);
        _attackVisualization.Color = new Color(1, 0, 0, 0.3f);
        _attackVisualization.Position = new Vector2(10, -30);
        AddChild(_attackVisualization);
        _attackVisualization.Visible = false;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // Base process handles timers
        base._PhysicsProcess(delta);
        
        // 2D movement input
        Vector2 velocity = Velocity;
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        
        if (inputDir != Vector2.Zero)
        {
            velocity.X = inputDir.X * Speed;
            velocity.Y = inputDir.Y * Speed;
            
            // Play Walk animation
            if (_animationPlayer != null && _attackTimer <= 0 && _hitStunTimer <= 0 && !_isPlayingActionAnimation)
            {
                if (!_animationPlayer.IsPlaying() || _animationPlayer.CurrentAnimation != "animations/Walk")
                {
                    PlayAnimation("animations/Walk");
                }
            }
            
            // Handle facing flip
            if (inputDir.X != 0)
            {
                FlipFacing(inputDir.X > 0);
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed * 2);
            velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed * 2);
            
            // Play Idle animation
            if (_animationPlayer != null && _attackTimer <= 0 && _hitStunTimer <= 0 && !_isPlayingActionAnimation)
            {
                if (!_animationPlayer.IsPlaying() || _animationPlayer.CurrentAnimation != "animations/Idle")
                {
                    PlayAnimation("animations/Idle");
                }
            }
        }
        
        ClampPositionToScreen();
        
        Velocity = velocity;
        MoveAndSlide();
        
        // Attack input
        if (Input.IsActionJustPressed("attack") && _attackTimer <= 0 && _hitStunTimer <= 0)
        {
            Attack();
        }
        
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            GetTree().Quit();
        }
    }
    
    private void Attack()
    {
        _attackTimer = AttackCooldown;
        
        if (_animationPlayer != null)
        {
            _isPlayingActionAnimation = true;
            PlayAnimation("animations/attack");
            GD.Print("[Animation] Playing attack animation");
        }
        
        GD.Print($"=== Player attacking! ===");
        
        int hitCount = 0;
        float facingDirection = _facingRight ? 1 : -1;
        
        var parent = GetParent();
        foreach (Node child in parent.GetChildren())
        {
            if (child is SampleEnemy enemy)
            {
                Vector2 playerPos = GlobalPosition;
                Vector2 enemyPos = enemy.GlobalPosition;
                Vector2 toEnemy = enemyPos - playerPos;
                float distance = toEnemy.Length();
                
                bool inRange = distance <= AttackRange;
                bool correctDirection = (facingDirection > 0 && toEnemy.X > 0) || 
                                       (facingDirection < 0 && toEnemy.X < 0);
                bool inVerticalRange = Mathf.Abs(toEnemy.Y) <= 80.0f;
                
                GD.Print($"Enemy at {enemyPos}, distance: {distance:F2}, direction OK: {correctDirection}, vertical OK: {inVerticalRange}");
                
                if (inRange && correctDirection && inVerticalRange)
                {
                    enemy.TakeDamage((int)AttackDamage);
                    hitCount++;
                    GD.Print($"Hit enemy! Distance: {distance:F2}");
                }
            }
        }
        
        if (hitCount == 0)
        {
            GD.Print("No enemies hit!");
        }
        
        if (_attackVisualization != null)
        {
            _attackVisualization.Visible = true;
            _attackVisualization.Position = _facingRight ? new Vector2(10, -30) : new Vector2(-90, -30);
            
            var vizTween = CreateTween();
            vizTween.TweenInterval(0.2);
            vizTween.TweenCallback(Callable.From(() => _attackVisualization.Visible = false));
        }
    }
    
    public override void TakeDamage(int damage)
    {
        // Call base for health deduction and animation
        base.TakeDamage(damage);
        UpdateStatsUI();
    }
    
    public void AddScore(int points)
    {
        _score += points;
        UpdateStatsUI();
    }
    
    private void UpdateStatsUI()
    {
        if (_statsLabel != null)
        {
            _statsLabel.Text = $"Player HP: {_currentHealth}\nScore: {_score}";
        }
    }
    
    protected override void Die()
    {
        GD.Print("Player died! Game Over!");
        GetTree().ReloadCurrentScene();
    }
}
