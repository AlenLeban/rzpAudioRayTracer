from matplotlib import pyplot as plt
import numpy as np

angles = np.arange(6, (3*20), 3) - 5
a0 = np.array([0.07358062, 0.05954827, 0.06409776, 0.05516685, 0.06134028, 0.05274491, 0.05907765, 0.05315689, 0.05896722, 0.054216, 0.0574876, 0.05529892, 0.05620211, 0.05599728, 0.05668093, 0.05607181, 0.05913623, 0.05568796])
a1 = np.array([0.07245981, 0.05760753, 0.06076606, 0.05114064, 0.05557125, 0.04675785, 0.05122036, 0.04517725, 0.04912312, 0.04435807, 0.04620821, 0.04374489, 0.04378237, 0.04302051, 0.04298817, 0.04203071, 0.04385688, 0.04089925])
a2 = np.array([0.07167923, 0.05619495, 0.05833808, 0.04824863, 0.05151711, 0.04265546, 0.04598612, 0.04000492, 0.04292066, 0.03831048, 0.03947114, 0.03701218, 0.03671701, 0.03579977, 0.03552524, 0.03452195, 0.0358268, 0.03325014])
a3 = np.array([0.06562181, 0.0492723, 0.04878401, 0.03837755, 0.03906615, 0.03105585, 0.03224239, 0.02721622, 0.02841101, 0.02480473, 0.02505447, 0.0231161, 0.02260562, 0.02177929, 0.02139117, 0.02060589, 0.02122467, 0.0195708])
normalizeFactor = 0.07358062
weights = np.ones((18))
weights[0] = 2
a0fit = np.poly1d(np.polyfit(angles, a0/normalizeFactor, 6, w=weights))
a1fit = np.poly1d(np.polyfit(angles, a1/normalizeFactor, 6, w=weights))
a2fit = np.poly1d(np.polyfit(angles, a2/normalizeFactor, 6, w=weights))
a3fit = np.poly1d(np.polyfit(angles, a3/normalizeFactor, 6, w=weights))
print(a0fit)
plt.figure()
plt.title("Diffraction frequency attenuation per angle")
plt.plot(angles, a0 / normalizeFactor, alpha=0.4, color="tab:orange")
plt.plot(angles, a1 / normalizeFactor, alpha=0.4, color="tab:blue")
plt.plot(angles, a2 / normalizeFactor, alpha=0.4, color="tab:green")
plt.plot(angles, a3 / normalizeFactor, alpha=0.4, color="tab:red")
plt.plot(angles, a0fit(angles), label="125Hz", color="tab:orange")
plt.plot(angles, a1fit(angles), label="500Hz", color="tab:blue")
plt.plot(angles, a2fit(angles), label="1000Hz", color="tab:green")
plt.plot(angles, a3fit(angles), label="4000Hz", color="tab:red")
plt.xlabel("Angle (in degrees)")
plt.ylabel("Attenuation factor")
plt.grid()
plt.legend()
plt.show()