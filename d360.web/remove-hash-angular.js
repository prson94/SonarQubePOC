const fs = require("fs")
const fileNamesStartsWith = ["runtime.","polyfills.","vendor.","main.","polyfills-es5."]
const mainDir = "./Scripts/dist/";
// Read directory
fs.readdir(mainDir, (err, folders) => {
  if (err)
    console.log(err);
  else {
    folders.forEach(folder => {
	   //each subdirectory is folder with localized chunks
	   var rootDir = mainDir + folder + "/";
	   fs.readdir(rootDir, (err, files) => {
	   for (const file of files) {
			 fileNamesStartsWith.forEach((sw)=>{
				 if(file.startsWith(sw)){
					 var renameFrom =rootDir + file;
					 var renameTo = rootDir + sw + "js";
					 fs.rename(renameFrom, renameTo, (err) => {
					 console.log('Renaming', renameFrom, " to ", renameTo)
					 if (err) throw err
				   })
				 }
			 })
		   }
	 });
    })
  }
});


