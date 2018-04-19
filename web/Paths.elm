module Paths exposing (..)

type Route
    = HomeRoute
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

aboutPath : Path
aboutPath =
    Path "about" AboutRoute
