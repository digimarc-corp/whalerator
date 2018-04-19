module Models exposing (..)

import Paths exposing (Route)
import Navigation

{-|
- `route` will hold the current matched route
- `changes` is just here to prove that we are not reloading the page and wiping out the app state
-}
type alias Model =
    { route : Route
    , changes : Int
    }

{-|
- ChangeLocation will be used for initiating a url change
- OnLocationChange will be triggered after a location change
-}
type Msg
    = ChangeLocation String
    | OnLocationChange Navigation.Location


{-|
initialModel will be called with the current matched route.
We store this in the model so we can display the corrent view.
-}
initialModel : Route -> Model
initialModel route =
    { route = route
    , changes = 0
    }
