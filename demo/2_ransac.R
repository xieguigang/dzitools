imports "geometry2D" from "graphics";
imports "machineVision" from "signalKit";

require(JSON);

let map = ?"--map" || stop("missing cells table data for make mapping!");
let subject = ?"--subj" || stop("missing subject cell table data for used as mapping reference!");
let dir = ?"--out" || relative_work();

map     = read.csv(map, row.names = 1, check.names = FALSE);
subject = read.csv(subject, row.names = 1, check.names = FALSE);

map = polygon2D(as.numeric(map$physical_x), as.numeric(map$physical_y));
subject = polygon2D(as.numeric(subject$physical_x), as.numeric(subject$physical_y));

let t = RANSAC(map, subject, iterations= 100000, threshold  = 1);

str(t);

let aligned = as.data.frame( geo_transform(map, t));

subject = as.data.frame(subject);
subject[,"class"] = "subject";
map = as.data.frame(map);
map[,"class"] = "map"; 

aligned[,"class"] = "aligned(map)";
aligned = rbind(subject, aligned);

let un_aligned = rbind(map, subject);

write.csv(aligned, file = file.path(dir, "RANSAC", "aligned.csv"), row.names = FALSE);
writeLines(JSON::json_encode(t), con = file.path(dir, "RANSAC", "transform.json"));

bitmap(file = file.path(dir, "RANSAC", "unaligned.png"), size = [3000,2000]) {
    plot(as.numeric(un_aligned$x),as.numeric(un_aligned$y), class = un_aligned$class, fill = "white",point_size= 5, colors = "paper");
}

bitmap(file = file.path(dir, "RANSAC", "aligned.png"), size = [3000,2000]) {
    plot(as.numeric(aligned$x),as.numeric(aligned$y), class = aligned$class, fill = "white",point_size= 5, colors = "paper");
}