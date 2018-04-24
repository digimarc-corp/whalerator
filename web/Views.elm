module Views exposing (..)

import Html exposing (..)
import Models exposing (..)
import Html.Events exposing (onWithOptions)
import Json.Decode as Decode
import Html.Attributes exposing (href)
import Paths exposing (..)


view : Model -> Html Msg
view model =
    div []
        [ nav model
        , page model
        ]


{-|
When clicking a link we want to prevent the default browser behaviour which is to load a new page.
So we use `onWithOptions` instead of `onClick`.
-}
onLinkClick : msg -> Attribute msg
onLinkClick message =
    let
        options =
            { stopPropagation = False
            , preventDefault = True
            }
    in
        onWithOptions "click" options (Decode.succeed message)


{-|
We want our links to show a proper href e.g. "/about", so we include an href attribute.
onLinkClick will prevent the browser reloading the page.
-}
nav : Model -> Html Msg
nav model =
    div []
        [ a [ href (rooted homePath), onLinkClick (ChangeLocation (rooted homePath)) ] [ text "Home" ]
        , text " | "
        , a [ href (rooted aboutPath), onLinkClick (ChangeLocation (rooted loginPath)) ] [ text "Login" ]
        , text " | "
        , a [ href (rooted aboutPath), onLinkClick (ChangeLocation (rooted aboutPath)) ] [ text "About" ]
        , text (" | " ++ toString model.changes)
        ]


{-|
Decide what to show based on the current `model.route`
-}
page : Model -> Html Msg
page model =
    case model.route of
        HomeRoute ->
            text "Home"

        LoginRoute ->
            text "Login"

        AboutRoute ->
            text "About"

        NotFoundRoute ->
            text "Not Found"

