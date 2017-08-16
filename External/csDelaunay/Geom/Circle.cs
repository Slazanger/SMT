using System.Collections;

namespace csDelaunay {
	public class Circle {

		public Vector2f center;
		public float radius;

		public Circle(float centerX, float centerY, float radius) {
			this.center = new Vector2f(centerX, centerY);
			this.radius = radius;
		}

		public override string ToString () {
			return "Circle (center: " + center + "; radius: " + radius + ")";
		}
	}
}