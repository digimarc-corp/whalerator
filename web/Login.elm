module Login exposing (..)

import Html exposing (..)
import Models exposing (..)
import Html.Events exposing (..)
import Json.Decode as Decode
import Html.Attributes exposing (..)
import Paths exposing (..)

type Msg
    = Username String
    | Password String
    | Registry String
    | Submit

loginView : Model -> LoginModel -> Html Msg
loginView model subModel =
    case model.session of
        Nothing ->
            div [] [ text "Time to get yourself a session?" ]
        Just session ->
            div [] [ text session ]

loginForm : Html Msg
loginForm =
    div []
        [ input [ type_ "text", placeholder "Name", onInput Username ] []
        , input [ type_ "password", placeholder "Password", onInput Password ] []
        , input [ type_ "text", placeholder "Registry", onInput Registry ] []
        --, viewValidation model
        ]