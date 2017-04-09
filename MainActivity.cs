using Android.App;
using Android.Widget;
using Android.OS;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Locations;
using Android.Runtime;
using System;
using Android.Gms.Common.Apis;
using Android.Gms.Common;
using Android.Gms.Location;
using System.Text;
using System.Collections.Generic;
using Android.Content;

namespace DroidMapping
{
    [Activity(Label = "DroidMapping", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, IOnMapReadyCallback, Android.Locations.ILocationListener, Android.Gms.Location.ILocationListener,
	                            GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener
	{
        GoogleMap map;
        MapFragment mapFragment;

		GoogleApiClient apiClient;

		// readonly GeofenceTriggerReceiver receiver = new GeofenceTriggerReceiver();

		// ---------------------------------------------------------------------------------------------------------------------------------- //

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

			// Register our broadcast receiver
			//IntentFilter filter = new IntentFilter(GeofenceTriggerReceiver.IntentName);
			//RegisterReceiver(receiver, filter);

            // Find and load the map fragment
            mapFragment = FragmentManager.FindFragmentById(Resource.Id.map) as MapFragment;
            mapFragment.GetMapAsync(this);

			// After the map fragment is setup, use the GoogleApiClient.Builder fluid API to create and assign the apiClient field.
			if (GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this) == 0)
			{
				apiClient = new GoogleApiClient.Builder(this)
					.AddConnectionCallbacks(this)
					.AddOnConnectionFailedListener(this)
					.AddApi(LocationServices.API)
					.Build();
			}
        }

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		/* This is called after OnCreate and indicates that your activity is running.
		   Check the apiClient, if it's not null, then call Connect on it.
		   Since we don't want location information before our map is visible, add a check before you call Connect to 
		   see if the map field is null. If it is, don't call Connect, instead, we'll call it in our OnMapReady override. */
		
		protected override void OnStart()
		{
			base.OnStart();
			if (apiClient != null && map != null)
			{
				apiClient.Connect();
			}
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		// If the apiClient field is valid, call Disconnect on it to turn off the API.

		protected override void OnStop()
		{
			base.OnStop();
			if (apiClient != null)
			{
				apiClient.Disconnect();
			}
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		/* This is called when the map has been loaded and is ready to be displayed in our fragment. 
		   This is where we will request location updates. */
		
        public void OnMapReady(GoogleMap googleMap)
        {
			map = googleMap;

            map.MapType = GoogleMap.MapTypeHybrid;
            map.UiSettings.ZoomControlsEnabled = true;

            Criteria criteria = new Criteria
			{
				Accuracy = Accuracy.Fine,
				PowerRequirement = Power.NoRequirement,
			};

			 /* Check the apiClient and if it's not null, then check the IsConnected property and call Connect if necessary.
			   Otherwise, if apiClient is null, then register with the LocationManager for updates. */
            if (apiClient != null) 
			{
                if (!apiClient.IsConnected)
                    apiClient.Connect();
            }
            else 
			{
                LocationManager locManager = LocationManager.FromContext(this);

                locManager.RequestLocationUpdates(5000, 100f, criteria, this, null);
            }
         /* Add an event handler for the GoogleMap.MapClick event at the end of the method. 
            Since we have some non-trivial work to do, it's recommended that you use a regular 
            method as the event handler (not a lambda). */
            map.MapClick += OnGetDetails;

			map.MapLongClick += ShowPOI;

        }

        // ---------------------------------------------------------------------------------------------------------------------------------- //

        List<Marker> pointsOfInterest = new List<Marker>();

        async void ShowPOI(object sender, GoogleMap.MapLongClickEventArgs e)
        {
	        LatLngBounds bounds = e.Point.GetBoundingBox(8000); // ~5mi

	        pointsOfInterest.ForEach(m => m.Remove());
	        pointsOfInterest.Clear();

	        Geocoder geocoder = new Geocoder(this);
	        var results = await geocoder.GetFromLocationNameAsync("Starbucks", 10,
					            bounds.Southwest.Latitude, bounds.Southwest.Longitude,
					            bounds.Northeast.Latitude, bounds.Northeast.Longitude);
	        foreach (var result in results)
	        {
		        var markerOptions = new MarkerOptions()
			                 .SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueCyan))
			                 .SetPosition(new LatLng(result.Latitude, result.Longitude))
			                 .SetTitle(result.FeatureName)
			                 .SetSnippet(GetAddress(result));
				
		        pointsOfInterest.Add(map.AddMarker(markerOptions));
	        }
        }

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		Marker lastGeoMarker;

		async void OnGetDetails(object sender, GoogleMap.MapClickEventArgs e)
		{
			/* The Geocoder class is used to issue address queries to Google's servers, 
			   and to receive a specific Address object */
			Geocoder geocoder = new Geocoder(this);

			// Use the GetFromLocationAsync method to return a single result for the passed point in the MapClickedEventArgs.
			var results = await geocoder.GetFromLocationAsync(e.Point.Latitude, e.Point.Longitude, 1);

			/* If the method returns a value (check the returning array's count), then 
			   take the resulting Address object and create a new Marker to display it on the map. */
			if (results.Count > 0)
			{
				var result = results[0];

				if (lastGeoMarker == null)
				{
					var markerOptions = new MarkerOptions()
						.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueOrange))
						.SetPosition(new LatLng(result.Latitude, result.Longitude))
						.SetTitle(result.FeatureName)
						.SetSnippet(GetAddress(result));
					
					lastGeoMarker = map.AddMarker(markerOptions);
				}
				else
				{
					lastGeoMarker.Position = new LatLng(result.Latitude, result.Longitude);
					lastGeoMarker.Title = result.FeatureName;
					lastGeoMarker.Snippet = GetAddress(result);
				}

				lastGeoMarker.ShowInfoWindow();
			}
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		string GetAddress(Address result)
		{
			var sb = new StringBuilder();
			for (int index = 0; index < result.MaxAddressLineIndex; index++)
			{
				if (sb.Length > 0)
					sb.Append(", ");
				sb.Append(result.GetAddressLine(index));
			}
			return sb.ToString();
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		Marker currentLocationMarker;

		// Notification method. In this method, we want to reposition our camera to the passed location.

		public void OnLocationChanged(Location location)
		{
			LatLng coord = new LatLng(location.Latitude, location.Longitude);
			CameraUpdate update = CameraUpdateFactory.NewLatLngZoom(coord, 17);
			map.AnimateCamera(update);

			/* After you have animated the camera position, check the currentLocationMarker field against null. 
			   If it's not null, then change the Position property to be the LatLng you just created. */
			if (currentLocationMarker != null)
			{
				currentLocationMarker.Position = coord;
			}
			/* If it is null, then we need to create a new marker to place onto the map and assign the field - this is 
			   the first time through. Create a new MarkerOptions object and use the fluid API to set the values */
			else
			{
				var markerOptions = new MarkerOptions()
					.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueBlue))
					.SetPosition(coord)
					.SetTitle("Current Position")
					.SetSnippet("This is where you are now");
				currentLocationMarker = map.AddMarker(markerOptions);
			}
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		/* This is called when we make a connection to the Google Play servers. 
		   Here is where we want to register our desire to receive location updates. */
		
		public void OnConnected(Bundle connectionHint)
		{
			/* create a new LocationRequest object using the LocationRequest.Create builder method. 
			   Set it up to match our usage of location. */
			LocationRequest locationRequest = LocationRequest.Create()
											  .SetPriority(LocationRequest.PriorityHighAccuracy)
											  .SetInterval(5000)
											  .SetSmallestDisplacement(100f);

			/* Next, call RequestLocationUpdates on the LocationServices.FusedLocationApi property to setup location updates. 
			   You will need to pass in your API client, the location request and an ILocationListener callback - pass the 
			   activity reference ("this") for that parameter. */
			LocationServices.FusedLocationApi.RequestLocationUpdates(apiClient, locationRequest, this);

			/* Remember that LocationServices uses a different interface for ILocationListener, but the single method 
			   defined - OnLocationChanged, actually matches the original ILocationListener we've already defined. 
			   Since we want to execute the same logic regardless of where the update came from, we can just share the 
			   implementation by adding Android.Gms.Location.ILocationListener as one our activity implements. */
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		public void OnProviderDisabled(string provider)
		{
			//throw new NotImplementedException();
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		public void OnProviderEnabled(string provider)
		{
			//throw new NotImplementedException();
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
		{
			//throw new NotImplementedException();
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		public void OnConnectionSuspended(int cause)
		{
			//throw new NotImplementedException();
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //

		public void OnConnectionFailed(ConnectionResult result)
		{
			//throw new NotImplementedException();
		}

		// ---------------------------------------------------------------------------------------------------------------------------------- //
	}
}

