module Main exposing (..)

import Html exposing (..)
import Html.Attributes exposing (href)
import Html.Events exposing (onWithOptions)
import Json.Decode as Decode
import Navigation
import UrlParser

import Models exposing (..)
import Routing exposing (..)
import Views exposing (..)

-- MESSAGES


-- UPDATE


{-|
On `ChangeLocation` call `Navigation.newUrl` to create a command that will change the browser location.

`OnLocationChange` will be called each time the browser location changes.
In this case we store the new route in the Model.
-}
update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        ChangeLocation path ->
            ( { model | changes = model.changes + 1 }, Navigation.newUrl path )

        OnLocationChange location ->
            let
                newRoute =
                    parseLocation location
            in
                ( { model | route = newRoute }, Cmd.none )

-- PROGRAM


init : Navigation.Location -> ( Model, Cmd Msg )
init location =
    let
        currentRoute =
            parseLocation location
    in
        ( initialModel currentRoute, Cmd.none )


subscriptions : Model -> Sub Msg
subscriptions model =
    Sub.none


main : Program Never Model Msg
main =
    Navigation.program OnLocationChange
        { init = init
        , view = view
        , update = update
        , subscriptions = subscriptions
        }