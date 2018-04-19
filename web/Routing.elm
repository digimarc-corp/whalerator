module Routing exposing (..)

import Navigation exposing (Location)
import UrlParser exposing (..)
import Paths exposing (..)
--exposing (..)


{-|
Define how to match urls
-}
matchers : UrlParser.Parser (Route -> a) a
matchers =
    UrlParser.oneOf
        [ UrlParser.map homePath.route UrlParser.top
        , UrlParser.map aboutPath.route (UrlParser.s aboutPath.path)
        ]



{-|
Match a location given by the Navigation package and return the matched route.
-}
parseLocation : Navigation.Location -> Route
parseLocation location =
    case (UrlParser.parsePath matchers location) of
        Just route ->
            route

        Nothing ->
            NotFoundRoute


