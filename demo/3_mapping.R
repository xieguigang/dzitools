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

let assign = greedy_matches(map, subject);

assign = as.data.frame(assign);

print(assign);

write.csv(assign, file = relative_work("RANSAC/cell_matches.csv"), row.names = FALSE);