import numpy as np

# v1,40,0.909,0.138,201,42,40,198,364,40,false,1000,10,390,385,389,385,15,15,14

unitSize = 20
reflectivity = 0.909
scattering = 0.138
source = [200, 50]
receiver = [200, 350]
geometryString = ""
vertexCount = 50
radius = 200
for i in range(vertexCount):
    siny = round(np.sin(i/vertexCount * 2 * np.pi) * radius + 200)
    cosx = round(np.cos(i/vertexCount * 2 * np.pi) * radius + 200)
    geometryString += f",{siny},{cosx}"
print(f"v1,{unitSize},{reflectivity},{scattering},{source[0]},{source[1]},40,{receiver[0]},{receiver[1]},40,false,100{geometryString}")