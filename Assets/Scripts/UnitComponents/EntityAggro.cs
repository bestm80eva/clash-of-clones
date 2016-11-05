﻿using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Component that manages aggro on entities.  Who is this entity targeting?
/// </summary>
[RequireComponent(typeof(Entity))]
public class EntityAggro : MonoBehaviour 
{
    public Entity Target
    {
        get { return _target; }
    }
    [SerializeField]private Entity _target;

    private Entity _entity;

    void Awake()
    {
        _entity = GetComponent<Entity>();
        
        if(_entity == null)
        {
            Debug.LogWarning("Requires an entity.");
        }

        _entity.SpawnedEvent.AddListener(onSpawned);
    }

    private void onSpawned()
    {
        this.enabled = true;
    }

    void Update()
    {
        // Clear out dead or invalid aggro target.
        if(_target != null && _target.HP <= 0)
        {
            _target = null;
        }

        // If no aggro target, find one.
        if (_target == null)
        {
            var enemies = getAllEnemiesInRange(_entity.Definition.AggroRange);
            if (enemies.Length > 0)
            {
                Entity closestEnemy = null;
                float closestDistance = float.MaxValue;
                foreach (var enemy in enemies)
                {
                    if (enemy.HP > 0)
                    {
                        // Only attack if the entity is a type that this unit attacks.
                        if ((_entity.Definition.AttacksGroundUnits && !enemy.Definition.IsAirUnit)
                            || (_entity.Definition.AttacksAirUnits && enemy.Definition.IsAirUnit)
                            || enemy.Definition.IsBuilding)
                        {
                            var targetPositionIgnoreY = enemy.transform.position;
                            targetPositionIgnoreY.y = transform.position.y;

                            float distance = Vector3.Distance(transform.position, targetPositionIgnoreY);
                            if (distance < closestDistance)
                            {
                                closestEnemy = enemy;
                                closestDistance = distance;
                            }
                        }
                    }
                }

                _target = closestEnemy;
            }
        }
    }

    // Question, will this be sufficient for flying enemies?  Or do we want a box check?
    private Entity[] getAllEnemiesInRange(float radius)
    {
        Vector3 bottom, top;
        getCapsulePointsFromPosition(transform.position, out bottom, out top);

        Collider[] allColliders = Physics.OverlapCapsule(bottom, top, radius);
        List<Entity> enemies = new List<Entity>();
        foreach(var collider in allColliders)
        {
            var entity = collider.GetComponent<Entity>();

            // If it's an enemy entity
            if(entity != null && entity.Owner != _entity.Owner)
            {
                enemies.Add(entity);
            }
        }
        return enemies.ToArray();
    }

    public bool IsInSights(Transform targetTransform, bool isDirectional)
    {
        // If the entity needs to aim, we have to wait until the target is in our sights.
        // If the direction doesn't matter, we should be ready to fire.
        bool isInSights;
        if(isDirectional)
        {
            isInSights = IsInSights(targetTransform);
        }
        else
        {
            isInSights = true;
        }

        return isInSights;
    }

    public bool IsInSights(Transform targetTansform)
    {
        bool isInSights = false;
        if(targetTansform != null)
        {
            var ourDirection = transform.forward.normalized;

            var targetPositionIgnoreY = targetTansform.position;
            targetPositionIgnoreY.y = transform.position.y;

            var targetDirection = (targetPositionIgnoreY - transform.position).normalized;
            var dot = Vector3.Dot(ourDirection, targetDirection);

            isInSights = Mathf.Abs(dot) > (1f - Consts.directionThreshholdForProjectileShot);
        }

        return isInSights;
    }

    

    public Entity[] GetEnemiesInRange(PlayerModel enemyPlayer, float radius)
    {
        // Collect all the colliders in a sphere from the current position in the given radius
        Vector3 bottom, top;
        getCapsulePointsFromPosition(transform.position, out bottom, out top);

        Collider[] colliders = Physics.OverlapCapsule (bottom, top, radius, CombatUtils.EntityMask);

        List<Entity> entities = new List<Entity>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Entity targetEntity = colliders[i].GetComponent<Entity> ();

            // If there is no entity script attached to the gameobject, go on to the next collider.
            if (targetEntity != null)
            {
                // If it's an enemy unit, do damage to it.
                if(targetEntity.Owner == enemyPlayer)
                {
                    entities.Add(targetEntity);
                }
            }
        }

        return entities.ToArray();
    }

    // Gets a capsule at the specified position that is ... large... on the y axis.
    private static void getCapsulePointsFromPosition(Vector3 position, out Vector3 point1, out Vector3 point2)
    {
        point1 = position + new Vector3(0f, -100f, 0f);
        point2 = position + new Vector3(0f, 100f, 0f);
    }
}