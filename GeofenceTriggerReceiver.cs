using Android.App;
using Android.Content;
using Android.Util;
using Android.Locations;
using Android.Gms.Location;
using System.Collections.Generic;
using System;

namespace DroidMapping
{
    [BroadcastReceiver(Exported=false)]
    [IntentFilter(new[] { GeofenceTriggerReceiver.IntentName })]
    public class GeofenceTriggerReceiver : BroadcastReceiver
    {
        readonly Dictionary<int,Tuple<Action<int>,Action<int>>> actions 
                = new Dictionary<int, Tuple<Action<int>, Action<int>>>();

        public const string IntentName = "com.xamarin.droidmapping.geofence";

// ---------------------------------------------------------------------------------------------------------------------------------- //

        public void RegisterActions(int key, Action<int> enterAction, Action<int> exitAction)
        {
            var work = Tuple.Create(enterAction, exitAction);
            actions.Add(key, work);
        }

// ---------------------------------------------------------------------------------------------------------------------------------- //

        public void UnregisterActions(int key)
        {
            actions.Remove(key);           
        }

// ---------------------------------------------------------------------------------------------------------------------------------- //

        public override void OnReceive(Context context, Intent intent)
        {
            bool entering = intent.GetBooleanExtra(LocationManager.KeyProximityEntering, false);

            GeofencingEvent geofencingEvent = GeofencingEvent.FromIntent(intent);

            if (geofencingEvent != null) 
			{
                entering = geofencingEvent.GeofenceTransition == Geofence.GeofenceTransitionEnter;   

                IList<IGeofence> crossedFences = geofencingEvent.TriggeringGeofences;

                Location location = geofencingEvent.TriggeringLocation;

                Log.Debug(GetType().Name, 
                   string.Format("Entered at ({0},{1} and crossed {2} fence(s).",
                    location.Latitude, location.Longitude, crossedFences.Count));
            }

            var extras = intent.GetBundleExtra(IntentName);

            string poiName = extras.GetString("name");

            int id = extras.GetInt("id");

            Tuple<Action<int>,Action<int>> work;

            actions.TryGetValue(id, out work);

            if (entering) 
			{
                Log.Debug(GetType().Name, "Entering " + poiName + " - " + id);

                if (work != null && work.Item1 != null) 
				{
                    work.Item1(id);
                }
            }
            else 
			{
                Log.Debug(GetType().Name, "Exiting "  + poiName + " - " + id);

                if (work != null && work.Item2 != null) 
				{
                    work.Item2(id);
                }
            }
        }

// ---------------------------------------------------------------------------------------------------------------------------------- //
    }
}