module Paths exposing (..)

type Route
    = HomeRoute
    | LoginRoute
    | AboutRoute
    | NotFoundRoute

type alias Path = 
    {   path : String
    ,   route : Route
    }

rooted : Path -> String
rooted path =
    "/" ++ path.path

homePath : Path
homePath =
    Path "" HomeRoute

loginPath : Path
loginPath =
    Path "login" LoginRoute

aboutPath : Path
aboutPath =
    Path "about" AboutRoute
