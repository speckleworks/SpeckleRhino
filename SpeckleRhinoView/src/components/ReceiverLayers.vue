<template>
  <v-container fluid class='layer-list'>
    <layer v-for='layer in layers' :key='layer.guid' :layer='layer' :clientId='clientId' class='layer'></layer>
  </v-container>
</template>

<script>
  import Layer from './ReceiverLayersLayer.vue'

  export default {
    name: 'ReceiverLayers',
    props: {
      layers: Array,
      clientId: String
    },
    components: {
      Layer
    },
    computed: {},
    data() {
      return {
      }
    },
    methods: {
    },
    mounted() {
    }
  }
</script>

<style lang='scss'>
  .layer-list{
    padding: 0;
  }

  .layer {
    padding: 2px 0 2px 0;
    background-color: rgba(25,25,25,0.3);
    /*border-left: 2px solid #0080FF;*/
    transition: all .3s ease;
  }

  .layer:hover {
    background-color: rgba(25,25,25,0.5);
    /*background-color: #CCCCCC;*/
    /*border-left: 2px solid white;*/
  }
</style>