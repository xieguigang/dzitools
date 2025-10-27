require(Erica);

imports "geometry2D" from "graphics";
imports "singleCell" from "Erica";

let t = relative_work("RANSAC/transform.json") 
|> readText() 
|> JSON::json_decode(typeof = "affine2d_transform")
;

str(t);

let map = read.cells(relative_work("modified/cells_bin.csv"));
let subject = read.cells(relative_work("capture/cells_bin.csv"));

map = map |> singleCell::geo_transform(t);

let assign = hungarian_assignment(map, subject);