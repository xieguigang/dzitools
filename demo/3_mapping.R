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
let df = as.data.frame(assign);

print(df);

write.csv(df, file = relative_work("RANSAC/cell_matches.csv"), row.names = FALSE);

bitmap(file = relative_work("RANSAC/cell_matches.png"), size = [2700,2100], dpi = 300) {
    plot(assign, slide1 = map, slide2 = subject);
}