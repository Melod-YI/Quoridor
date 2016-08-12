package mathUtil;

public class UDGraph {
	private int[][] E;
	private int VNum; 
	public UDGraph(int[][] E){
		this.E=E;
		VNum=E.length;
	}
	
	public void Set(int x,int y){
		E[x-1][y-1]=1;
		E[y-1][x-1]=1;
	}
	
	public void Remove(int x,int y){
		E[x-1][y-1]=0;
		E[y-1][x-1]=0;
	}
	
	public boolean RouteTest(int start,int[] end,int[] EWatched){
		boolean result=false;
		for(int i=0;i<VNum;i++){
			if(E[start][i]==1&&EWatched[i]==0){
				System.out.print(i+" ");
				for(int k=0;k<end.length;k++){
					if(end[k]==i){
						System.out.println();
						result=true;
					}
					else{
						EWatched[i]=1;
						result=result||RouteTest(i,end,EWatched);
					}
				}
			}
		}
		System.out.println();
		return result;
	}
	
	public boolean startTest(int start,int[] end){
		
		int[] EWatched=new int[VNum];
		for(int i=0;i<VNum;i++){
			EWatched[i]=0;
		}
		EWatched[start]=1;
		boolean result=RouteTest(start,end,EWatched);
		return result;
	}
	
//	public static void main(String[] args) {
//		int[] end ={73,74,75,76,77,78,79,80,81};
//		int[][] graph=new int[81][81];
//		for(int i=0;i<81;i++){
//			for(int j=0;j<81;j++){
//				if(j==i+9||j==i-9||j==i+1||j==i-1){
//					graph[i][j]=1;
//				}
//			}
//		}
//		for(int i=1;i<9;i++){
//			graph[i*9][i*9-1]=0;
//			graph[i*9-1][i*9]=0;
//		}
//		for(int i=72;i<81;i++){
//			graph[i][i-9]=0;
//			graph[i-9][i]=0;
//		}
//		for(int i=0;i<81;i++){
//			for(int j=0;j<81;j++){
//				System.out.print(graph[i][j]);
//			}
//			System.out.println();
//		}	
//		UDGraph test=new UDGraph(graph);
//		System.out.println(test.startTest(0, end));
//	}
	
}
