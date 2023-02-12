# How to use `git` with this project

**Important:** Do **NOT** make a new Unity project each time you want to work on this. It is **a lot of effort** to integrate after the fact. It is **faster** and **easier** to follow this guide.

### **Step 0 -** `git clone` the repository

*You only need to do this the first time.*

```bash
$ git clone https://github.com/NPCMS/ElementalExplorers.git
$ cd ElementalExplorers
```

### **Step 1 -** Move to the [`dev`](https://github.com/NPCMS/ElementalExplorers/tree/dev) branch

```bash
$ git checkout -b dev origin/dev
```

### **Step 2 -** Pull the latest changes

```bash
$ git pull --ff-only
```
Don't omit the `--ff-only` flag, as it may cause a messy git history.

### **Step 3 -** Branch off [`dev`](https://github.com/NPCMS/ElementalExplorers/tree/dev)

```bash
$ git checkout -b <your-branch-name-here> dev
```

### **Step 4-** Setup Unity For Collaboration

- Open the editor settings window.  
```Edit > Project Settings > Editor```
- Make .meta files visible to avoid broken object references.  
```Version Control / Mode: “Visible Meta Files”```   
- Use plain text serialization to avoid unresolvable merge conflicts.  
```Asset Serialization / Mode: “Force Text”```  
- Save your changes.  
```File > Save Project```

### **Step 5 -** DO NOT COMMIT ALL CHANGES


When committing changes, follow the principle of **atomic commits**. Make each commit into a *single logical unit*. 
The changes made with each commit should be as minimal as possible in order to get the change working.  
Do not under any circumstances use the command before comitting changes:  
```bash
$ git add .
```
Instead, you should specifically add the files you want to commit to the repository. Failure to comply with this simple rule will result in merge conflicts which are time consuming to resolve.
More generally, it is wise to avoid working on the same [scene](https://docs.unity3d.com/560/Documentation/Manual/CreatingScenes.html) or [prefab](https://docs.unity3d.com/Manual/Prefabs.html) as someone else simultaneously. Instead you should pair-program with them :)


```bash
# -- after making changes to N files --
$ git add <file1> <file2> ... <fileN>
$ git commit -m "describe change" 
# -- importantly there is no -a flag in the commit here --
$ git push --set-upstream origin/<your-branch-name-here>
```

*You only need the `--set-upstream origin/<your-branch-name-here>` flag on your first commit.*

### **Step 6 -** Open a [pull request](https://github.com/NPCMS/ElementalExplorers/compare) to incorporate changes

Make sure to set the base to `dev`, and compare to the branch you created.

Wait for someone else to review your code and confirm your pull request, as there may be merge conflicts or other issues with your code that need to be fixed before it is included.

### **Step 7 -** AVOID STASHING

When you are working on the unity project, it is best to avoid using
```bash
$ git stash .
```
This is because Unity produces temporary files that may become corrupted when you stash and apply. This means that the entire editor may become unusable even if you reclone the project. 

Instead, if you have made changes, you should close the editor and use :
```bash
$ git reset --hard
```
Copy any scripts you want to keep before you perform this action then paste them back in when you reopen the editor.