using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class valueDefinitions : MonoBehaviour {

	/*
	 * Define all your values of the game here.
	 * The value definitions are used in several scripts to 
	 * show easy dropdown menues e.g. for conditions or value modifications.
	 */

	public enum values{
		name,
		surname,
		gender,
		country,
		years,
		bodyMind,
		academics,
		relationships,
		economy,
		authority,
		intelligence,
		charisma,
		luck,
		creativity,
		look,
		health,
		marriage,
		married,
		// Reserved to keep serialized enum ordinals stable.
		reservedValue18,
		gamesPlayed,
		showResult,
        moneyCredit,
        war,
        enemyArmy,
        playerLevel,
        reservedValue25,
        reservedValue26,
        mayor_year,
        mayor_people,
        mayor_party,
        mayor_police,
        mayor_money,
        mayor_level,
        mayor_avatar
	}
}
