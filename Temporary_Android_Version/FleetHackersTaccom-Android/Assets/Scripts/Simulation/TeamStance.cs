using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Need to get this converted to Scriptible Objects.
/// </summary>
[SerializeField]
public class TeamStance : MonoBehaviour
{
    [SerializeField]
    int _teamCode;

    [SerializeField]
    List<int> alliedGroups;

    [SerializeField]
    List<int> enemyGroups;

    // DONT MODIFY!
    [SerializeField]
    List<BasicShip> _ownedShips;

    [SerializeField]
    List<BasicShip> _alliedShips;

    [SerializeField]
    List<BasicShip> _enemyShips;

    //public TeamStance()
    //{

    //}

    void Awake()
    {
        GameObject[] allShips = GameObject.FindGameObjectsWithTag("Ship");

        foreach(var s in allShips)
        {
            var ship = s.GetComponent<BasicShip>();
            if(ship.GroupId == _teamCode)
            {
                _ownedShips.Add(ship);
                ship.TeamDatabase = this;
            }

            foreach(var allied in alliedGroups)
            {
                if(ship.GroupId == allied)
                {
                    _alliedShips.Add(ship);
                }
            }

            foreach(var enemy in enemyGroups)
            {
                if(ship.GroupId == enemy)
                {
                    _enemyShips.Add(ship);
                }
            }
        }
    }

    void Start()
    {

    }

    public List<BasicShip> AlliedShips
    {
        get
        {
            return _alliedShips;
        }

        set
        {
            _alliedShips = value;
        }
    }

    public List<BasicShip> OwnedShips
    {
        get
        {
            return _ownedShips;
        }

        set
        {
            _ownedShips = value;
        }
    }

    public List<BasicShip> EnemyShips
    {
        get
        {
            return _enemyShips;
        }

        set
        {
            _enemyShips = value;
        }
    }

    public int TeamCode
    {
        get
        {
            return _teamCode;
        }

        set
        {
            _teamCode = value;
        }
    }
}
